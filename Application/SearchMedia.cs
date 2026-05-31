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

        var item = searchResult.FirstOrDefault();

        if (item == null)
            return DisplayMediaResult.Failure("No media found for the given query.");
            
        var mediaContent = await mediaLoader.LoadAsync(item);

        return !mediaContent.Success ? DisplayMediaResult.Failure(mediaContent.ErrorMessage) : DisplayMediaResult.FromMedia(item.MediaType, mediaContent.Bytes);
    }
}