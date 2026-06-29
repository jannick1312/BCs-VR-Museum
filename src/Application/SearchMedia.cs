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

        var query = new SearchQuery(text, limit);
        var searchResult = await searchEngine.SearchAsync(query);

        if (!searchResult.Success)
            return DisplayMediaResult.Failure(searchResult.ErrorMessage);

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
            items.Add(new DisplayMediaItem(searchItems[i].MediaType, mediaContent.Bytes, mediaContent.Path, searchItems[i].Name));
        }
        _logger.Info($"Media search completed. SearchItems={searchItems.Count}, LoadedItems={items.Count}");
        return DisplayMediaResult.FromMedia(items);
    }
}