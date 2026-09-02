namespace Core;

/// <summary>
/// Represents a media search that uses text.
/// </summary>
/// <param name="text">The text to search for.</param>
/// <param name="limit">The maximum number of results to request.</param>
public sealed class TextSearchQuery(string text, int limit = 1) : SearchQuery(limit)
{
	public string Text { get; } = text;
}
