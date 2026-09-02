using Core;
using Logger;
using Models;

namespace Application;

/// <summary>
/// Searches for media and loads the results for display.
/// </summary>
/// <param name="searchEngine">The search engine used to find media.</param>
/// <param name="mediaLoader">The loader used to load the selected media.</param>
public class SearchMedia(ISearchEngine searchEngine, IMediaLoader mediaLoader)
{
	private readonly EventLogger _logger = new(nameof(SearchMedia));

	/// <summary>
	/// Searches for media that matches the text.
	/// </summary>
	/// <param name="text">The text to search for.</param>
	/// <param name="limit">The maximum number of results to return.</param>
	/// <param name="mediaMode">The types of media to include.</param>
	/// <param name="maxMedia2D">The maximum number of images and videos to load.</param>
	/// <param name="maxObjects3D">The maximum number of 3D models to load.</param>
	/// <returns>A task containing the loaded media result.</returns>
	public async Task<DisplayMediaResult> ExecuteAsync(string text, int limit, MediaMode mediaMode, int maxMedia2D, int maxObjects3D)
	{
		_logger.Info($"Text search started. Query='{text}', Limit={limit}, MediaMode={mediaMode}.");

		var query = SearchQuery.FromText(text, limit);
		return await ExecuteAsync(query, limit, mediaMode, maxMedia2D, maxObjects3D);
	}

	/// <summary>
	/// Searches for media that is similar to the feature vector.
	/// </summary>
	/// <param name="vector">The feature vector used for the similarity search.</param>
	/// <param name="limit">The maximum number of results to return.</param>
	/// <param name="mediaMode">The types of media to include.</param>
	/// <param name="maxMedia2D">The maximum number of images and videos to load.</param>
	/// <param name="maxObjects3D">The maximum number of 3D models to load.</param>
	/// <returns>A task containing the loaded media result.</returns>
	public async Task<DisplayMediaResult> ExecuteAsync(IReadOnlyList<double> vector, int limit, MediaMode mediaMode, int maxMedia2D, int maxObjects3D)
	{
		_logger.Info($"Similarity search started. VectorLength={vector.Count}, Limit={limit}, MediaMode={mediaMode}.");

		var query = SearchQuery.FromVector(vector, limit);
		return await ExecuteAsync(query, limit, mediaMode, maxMedia2D, maxObjects3D);
	}

	/// <summary>
	/// Runs a search and loads the selected media.
	/// </summary>
	/// <param name="query">The search to run.</param>
	/// <param name="limit">The maximum number of results to return.</param>
	/// <param name="mediaMode">The types of media to include.</param>
	/// <param name="maxMedia2D">The maximum number of images and videos to load.</param>
	/// <param name="maxObjects3D">The maximum number of 3D models to load.</param>
	/// <returns>A task containing the loaded media result.</returns>
	private async Task<DisplayMediaResult> ExecuteAsync(SearchQuery query, int limit, MediaMode mediaMode, int maxMedia2D, int maxObjects3D)
	{
		var searchResult = await searchEngine.SearchAsync(query);
		if (!searchResult.Success)
		{
			_logger.Warning($"Media search failed before loading. Error='{searchResult.ErrorMessage}'.");
			return DisplayMediaResult.Failure(searchResult.ErrorMessage);
		}

		var searchItems = SelectItems(searchResult.Items, limit, mediaMode, maxMedia2D, maxObjects3D);
		_logger.Info($"Loading media within placement capacity. Max2D={maxMedia2D}, Max3D={maxObjects3D}, SelectedItems={searchItems.Count}.");
		mediaLoader.BeginBatch();
		var loadTasks = searchItems.Select(mediaLoader.LoadAsync).ToArray();
		var mediaContents = await Task.WhenAll(loadTasks);
		mediaLoader.CommitBatch();

		var items = new List<DisplayMediaItem>();
		for (var i = 0; i < searchItems.Count; i++)
		{
			var mediaContent = mediaContents[i];

			if (!mediaContent.Success)
			{
				_logger.Warning($"Media item skipped because loading failed. Name='{searchItems[i].Name}', Error='{mediaContent.ErrorMessage}'.");
				continue;
			}

			items.Add(new DisplayMediaItem(searchItems[i].Vector, searchItems[i].MediaType, mediaContent.Path, searchItems[i].Name, searchItems[i].StartTimeSeconds, searchItems[i].Metadata));
		}

		_logger.Info($"Media search completed. SelectedItems={searchItems.Count}, LoadedItems={items.Count}.");
		return DisplayMediaResult.FromMedia(items);
	}

	/// <summary>
	/// Selects search results that match the media mode and placement limits.
	/// </summary>
	/// <param name="candidates">The search results to select from.</param>
	/// <param name="limit">The maximum number of items to select.</param>
	/// <param name="mediaMode">The types of media to include.</param>
	/// <param name="maxMedia2D">The maximum number of images and videos to select.</param>
	/// <param name="maxObjects3D">The maximum number of 3D models to select.</param>
	/// <returns>The selected search results.</returns>
	private static List<SearchResultItem> SelectItems(IReadOnlyList<SearchResultItem> candidates, int limit, MediaMode mediaMode, int maxMedia2D, int maxObjects3D)
	{
		var selected = new List<SearchResultItem>();
		var media2DCount = 0;
		var object3DCount = 0;

		foreach (var item in candidates)
		{
			if (selected.Count >= limit)
				break;
			if (!IsAllowed(item.MediaType, mediaMode))
				continue;

			if (item.MediaType is MediaType.Image or MediaType.Video)
			{
				if (media2DCount >= maxMedia2D)
					continue;
				media2DCount++;
			}
			else if (item.MediaType == MediaType.Object3D)
			{
				if (object3DCount >= maxObjects3D)
					continue;
				object3DCount++;
			}

			selected.Add(item);
		}

		return selected;
	}

	/// <summary>
	/// Checks if a media type is allowed by the selected media mode.
	/// </summary>
	/// <param name="mediaType">The media type to check.</param>
	/// <param name="mediaMode">The selected media mode.</param>
	/// <returns><see langword="true"/> if the media type is allowed and <see langword="false"/> otherwise.</returns>
	private static bool IsAllowed(MediaType mediaType, MediaMode mediaMode)
	{
		return mediaMode switch
		{
			MediaMode.Images => mediaType is MediaType.Image or MediaType.Video,
			MediaMode.Objects3D => mediaType == MediaType.Object3D,
			_ => mediaType is MediaType.Image or MediaType.Video or MediaType.Object3D
		};
	}
}



// Codex helped implement the logic that selects how many images, videos, and 3D models can be loaded.
