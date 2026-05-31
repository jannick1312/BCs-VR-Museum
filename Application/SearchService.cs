using Core;

namespace Application;

public abstract class SearchService
{
    public abstract Task<SearchResult> SearchAsync(SearchQuery query);
}