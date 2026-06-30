namespace Core;

public class SearchQuery(string text, int limit = 1)
{
	public string Text { get; } = text;
	public int Limit { get; } = limit;
}