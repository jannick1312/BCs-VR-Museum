namespace Core;

public class SearchResult
{
    public bool Success { get; }
    public string ErrorMessage { get; }
    private IReadOnlyList<SearchResultItem> Items { get; }

    private SearchResult(bool success, IReadOnlyList<SearchResultItem> items, string errorMessage)
    {
        Success = success;
        Items = items;
        ErrorMessage = errorMessage;
    }

    public static SearchResult FromItems(IReadOnlyList<SearchResultItem> items)
    {
        return new SearchResult(true, items, "");
    }

    public static SearchResult Failure(string errorMessage)
    {
        return new SearchResult(false, [], errorMessage);
    }

    public SearchResultItem? FirstOrDefault()
    {
        return Items.Count > 0 ? Items[0] : null;
    }
}