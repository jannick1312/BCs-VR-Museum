namespace Core;

/// <summary>
/// Represents a media search that uses a feature vector.
/// </summary>
/// <param name="vector">The feature vector used for the search.</param>
/// <param name="limit">The maximum number of results to request.</param>
public sealed class VectorSearchQuery(IReadOnlyList<double> vector, int limit = 1) : SearchQuery(limit)
{
	public IReadOnlyList<double> Vector { get; } = vector;
}
