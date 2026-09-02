namespace Core;

/// <summary>
/// Represents a media search query.
/// </summary>
/// <param name="limit">The maximum number of results to request.</param>
public abstract class SearchQuery(int limit)
{
	public int Limit { get; } = limit;

	/// <summary>
	/// Creates a text search query.
	/// </summary>
	/// <param name="text">The text to search for.</param>
	/// <param name="limit">The maximum number of results to request.</param>
	/// <returns>The text search query.</returns>
	public static SearchQuery FromText(string text, int limit = 1)
	{
		return new TextSearchQuery(text, limit);
	}

	/// <summary>
	/// Creates a vector similarity search query.
	/// </summary>
	/// <param name="vector">The vector used for the search.</param>
	/// <param name="limit">The maximum number of results to request.</param>
	/// <returns>The vector search query.</returns>
	public static SearchQuery FromVector(IReadOnlyList<double> vector, int limit = 1)
	{
		return new VectorSearchQuery(vector, limit);
	}
}
