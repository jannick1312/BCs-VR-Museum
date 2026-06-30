namespace Core;

public sealed class TextSearchQuery(string text, int limit = 1) : SearchQuery(limit)
{
	public string Text { get; } = text;
}