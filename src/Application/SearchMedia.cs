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

        var items = new List<DisplayMediaItem>();

        foreach (var item in searchResult.Items.Take(limit))
        {
            var mediaContent = await mediaLoader.LoadAsync(item);
            items.Add(new DisplayMediaItem(item.MediaType, mediaContent.Bytes));
        }

        return DisplayMediaResult.FromMedia(items);
    }
}