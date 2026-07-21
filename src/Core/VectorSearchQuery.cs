namespace Core;

public sealed class VectorSearchQuery(IReadOnlyList<double> vector, int limit = 1) : SearchQuery(limit)
{
	public IReadOnlyList<double> Vector { get; } = vector;
}
