namespace Models;

public class DisplayMediaResult
{
    public bool Success { get; }
    public IReadOnlyList<DisplayMediaItem> Items { get; }

    private DisplayMediaResult(bool success, IReadOnlyList<DisplayMediaItem> items)
    {
        Success = success;
        Items = items;
    }

    public static DisplayMediaResult FromMedia(IReadOnlyList<DisplayMediaItem> items)
    {
        return new DisplayMediaResult(true, items);
    }
}