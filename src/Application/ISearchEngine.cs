using Core;

namespace Application;

public interface ISearchEngine
{
	Task<SearchResult> SearchAsync(SearchQuery query);
}
