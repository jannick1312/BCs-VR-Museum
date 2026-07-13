namespace Models;

public class DisplayMediaResult
{
	private DisplayMediaResult(bool success, IReadOnlyList<DisplayMediaItem> items, string errorMessage)
	{
		Success = success;
		Items = items;
		ErrorMessage = errorMessage;
	}

	public bool Success { get; }
	public IReadOnlyList<DisplayMediaItem> Items { get; }
	public string ErrorMessage { get; }

	public static DisplayMediaResult FromMedia(IReadOnlyList<DisplayMediaItem> items)
	{
		return new DisplayMediaResult(true, items, "");
	}

	public static DisplayMediaResult Failure(string errorMessage)
	{
		return new DisplayMediaResult(false, [], errorMessage);
	}
}
