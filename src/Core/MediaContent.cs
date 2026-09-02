namespace Core;

/// <summary>
/// Represents the result of loading a media file.
/// </summary>
public class MediaContent
{
	/// <summary>
	/// Creates a media loading result.
	/// </summary>
	/// <param name="success">If the media was loaded successfully.</param>
	/// <param name="path">The path to the loaded media file.</param>
	/// <param name="errorMessage">The error message when loading failed.</param>
	private MediaContent(bool success, string path, string errorMessage)
	{
		Success = success;
		Path = path;
		ErrorMessage = errorMessage;
	}

	public bool Success { get; }
	public string Path { get; }
	public string ErrorMessage { get; }

	/// <summary>
	/// Creates a successful result for a loaded media file.
	/// </summary>
	/// <param name="path">The path to the loaded media file.</param>
	/// <returns>A successful media loading result.</returns>
	public static MediaContent FromPath(string path)
	{
		return new MediaContent(true, path, "");
	}

	/// <summary>
	/// Creates a failed media loading result.
	/// </summary>
	/// <param name="errorMessage">The message describing the failure.</param>
	/// <returns>A failed media loading result.</returns>
	public static MediaContent Failure(string errorMessage)
	{
		return new MediaContent(false, "", errorMessage);
	}
}
