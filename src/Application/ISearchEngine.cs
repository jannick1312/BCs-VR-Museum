using Core;

namespace Application;

/// <summary>
/// Searches for media that matches a query.
/// </summary>
public interface ISearchEngine
{
	/// <summary>
	/// Runs a media search.
	/// </summary>
	/// <param name="query">The search to run.</param>
	/// <returns>A task containing the search result.</returns>
	Task<SearchResult> SearchAsync(SearchQuery query);
}
