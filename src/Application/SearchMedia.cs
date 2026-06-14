using Core;

namespace Application;

public class SearchMedia(ISearchService searchService, IMediaLoader mediaLoader)
{
    public async Task<DisplayMediaResult> ExecuteAsync(string text, int limit)
    {
        var query = new SearchQuery(text, limit);

        var searchResult = await searchService.SearchAsync(query);

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
                continue;

            items.Add(new DisplayMediaItem(searchItems[i].MediaType, mediaContent.Bytes));
        }

        return DisplayMediaResult.FromMedia(items);
    }
}