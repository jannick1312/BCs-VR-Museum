using Core;

namespace Application;

public class SearchAndLoadMedia(ISearchService searchService, IMediaLoader mediaLoader)
{
    private readonly ISearchService _searchService = searchService;
    private readonly IMediaLoader _mediaLoader = mediaLoader;

    public async Task<DisplayMediaResult> ExecuteAsync(string text, int limit)
    {
        var query = new SearchQuery(text, limit);

        var searchResult = await _searchService.SearchAsync(query);

        if (!searchResult.Success)
            return DisplayMediaResult.Failure(searchResult.ErrorMessage);

        var item = searchResult.FirstOrDefault();

        if (item == null)
            return DisplayMediaResult.Failure("Search returned no result item.");

        var mediaContent = await _mediaLoader.LoadAsync(item);

        if (!mediaContent.Success)
            return DisplayMediaResult.Failure(mediaContent.ErrorMessage);

        return DisplayMediaResult.FromMedia(item.MediaType, mediaContent.Bytes, item.FileName, mediaContent.Source);
    }
}