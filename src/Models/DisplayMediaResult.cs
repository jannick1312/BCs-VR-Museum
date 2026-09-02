namespace Models;

/// <summary>
/// Represents the result of a media search and its loaded items.
/// </summary>
public class DisplayMediaResult
{
	/// <summary>
	/// Creates a media result.
	/// </summary>
	/// <param name="success">If the media search succeeded.</param>
	/// <param name="items">The media items that were loaded successfully.</param>
	/// <param name="errorMessage">The error message when the search failed.</param>
	private DisplayMediaResult(bool success, IReadOnlyList<DisplayMediaItem> items, string errorMessage)
	{
		Success = success;
		Items = items;
		ErrorMessage = errorMessage;
	}

	public bool Success { get; }
	public IReadOnlyList<DisplayMediaItem> Items { get; }
	public string ErrorMessage { get; }

	/// <summary>
	/// Creates a successful result with the loaded media items.
	/// </summary>
	/// <param name="items">The loaded media items.</param>
	/// <returns>A successful display result.</returns>
	public static DisplayMediaResult FromMedia(IReadOnlyList<DisplayMediaItem> items)
	{
		return new DisplayMediaResult(true, items, "");
	}

	/// <summary>
	/// Creates a failed media result.
	/// </summary>
	/// <param name="errorMessage">The message describing the failure.</param>
	/// <returns>A failed display result.</returns>
	public static DisplayMediaResult Failure(string errorMessage)
	{
		return new DisplayMediaResult(false, [], errorMessage);
	}
}
