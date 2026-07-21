using Core;
using Logger;
using Models;

namespace Application;

public class SearchMedia(ISearchEngine searchEngine, IMediaLoader mediaLoader)
{
	private readonly EventLogger _logger = new(nameof(SearchMedia));

	public async Task<DisplayMediaResult> ExecuteAsync(string text, int limit, MediaMode mediaMode, int maxMedia2D, int maxObjects3D)
	{
		_logger.Info($"Text search started. Query='{text}', Limit={limit}, MediaMode={mediaMode}.");

		var query = SearchQuery.FromText(text, limit);
		return await ExecuteAsync(query, limit, mediaMode, maxMedia2D, maxObjects3D);
	}

	public async Task<DisplayMediaResult> ExecuteAsync(IReadOnlyList<double> vector, int limit, MediaMode mediaMode, int maxMedia2D, int maxObjects3D)
	{
		_logger.Info($"Similarity search started. VectorLength={vector.Count}, Limit={limit}, MediaMode={mediaMode}.");

		var query = SearchQuery.FromVector(vector, limit);
		return await ExecuteAsync(query, limit, mediaMode, maxMedia2D, maxObjects3D);
	}

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

			items.Add(new DisplayMediaItem(searchItems[i].Vector, searchItems[i].MediaType, mediaContent.Path, searchItems[i].Name));
		}

		_logger.Info($"Media search completed. SelectedItems={searchItems.Count}, LoadedItems={items.Count}.");
		return DisplayMediaResult.FromMedia(items);
	}

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
