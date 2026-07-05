using Core;
using Models;
using Logger;

namespace Application;

public class SearchMedia(ISearchEngine searchEngine, IMediaLoader mediaLoader)
{
	private readonly EventLogger _logger = new(nameof(SearchMedia));

	public async Task<DisplayMediaResult> ExecuteAsync(string text, int limit)
	{
		_logger.Info($"Executing media search. Query='{text}', Limit={limit}");

		var query = SearchQuery.FromText(text, limit);
		return await ExecuteAsync(query, limit);
	}

	public async Task<DisplayMediaResult> ExecuteAsync(IReadOnlyList<double> vector, int limit)
	{
		_logger.Info($"Executing media search. VectorLength={vector.Count}, Limit={limit}");

		var query = SearchQuery.FromVector(vector, limit);
		return await ExecuteAsync(query, limit);
	}

	private async Task<DisplayMediaResult> ExecuteAsync(SearchQuery query, int limit)
	{
		var searchResult = await searchEngine.SearchAsync(query);

		var searchItems = searchResult.Items.Take(limit).ToList();
		var loadTasks = searchItems.Select(mediaLoader.LoadAsync).ToArray();
		var mediaContents = await Task.WhenAll(loadTasks);

		var items = new List<DisplayMediaItem>();
		for (var i = 0; i < searchItems.Count; i++)
		{
			var mediaContent = mediaContents[i];

			if (!mediaContent.Success)
			{
				_logger.Warning($"Skipping media item because loading failed: {mediaContent.ErrorMessage}");
				continue;
			}
			var path = mediaContent.Path.Length > 0 ? mediaContent.Path : searchItems[i].RemoteUrl;
			items.Add(new DisplayMediaItem(searchItems[i].Vector, searchItems[i].MediaType, mediaContent.Bytes, path, searchItems[i].Name));
		}
		_logger.Info($"Media search completed. SearchItems={searchItems.Count}, LoadedItems={items.Count}");
		return DisplayMediaResult.FromMedia(items);
	}
}