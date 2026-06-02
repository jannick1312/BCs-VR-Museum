using Core;

namespace Application;

public interface ISearchService
{
    Task<SearchResult> SearchAsync(SearchQuery query);
}