namespace Core;

public abstract class SearchQuery(int limit)
{
	public int Limit { get; } = limit;

	public static SearchQuery FromText(string text, int limit = 1)
	{
		return new TextSearchQuery(text, limit);
	}

	public static SearchQuery FromVector(IReadOnlyList<double> vector, int limit = 1)
	{
		return new VectorSearchQuery(vector, limit);
	}
}