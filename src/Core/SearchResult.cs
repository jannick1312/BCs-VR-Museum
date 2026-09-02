namespace Core;

/// <summary>
/// Represents the result of a media search.
/// </summary>
public class SearchResult
{
	/// <summary>
	/// Creates a media search result.
	/// </summary>
	/// <param name="success">If the search completed successfully.</param>
	/// <param name="items">The items returned by the search.</param>
	/// <param name="errorMessage">The error message when the search failed.</param>
	private SearchResult(bool success, IReadOnlyList<SearchResultItem> items, string errorMessage)
	{
		Success = success;
		Items = items;
		ErrorMessage = errorMessage;
	}

	public bool Success { get; }
	public string ErrorMessage { get; }
	public IReadOnlyList<SearchResultItem> Items { get; }

	/// <summary>
	/// Creates a successful result with the found media items.
	/// </summary>
	/// <param name="items">The items returned by the search.</param>
	/// <returns>A successful search result.</returns>
	public static SearchResult FromItems(IReadOnlyList<SearchResultItem> items)
	{
		return new SearchResult(true, items, "");
	}

	/// <summary>
	/// Creates a failed search result.
	/// </summary>
	/// <param name="errorMessage">The message describing the failure.</param>
	/// <returns>A failed search result.</returns>
	public static SearchResult Failure(string errorMessage)
	{
		return new SearchResult(false, [], errorMessage);
	}
}
