namespace Core;

public class MediaContent
{
	private MediaContent(bool success, string path, string errorMessage)
	{
		Success = success;
		Path = path;
		ErrorMessage = errorMessage;
	}

	public bool Success { get; }
	public string Path { get; }
	public string ErrorMessage { get; }

	public static MediaContent FromPath(string path)
	{
		return new MediaContent(true, path, "");
	}

	public static MediaContent Failure(string errorMessage)
	{
		return new MediaContent(false, "", errorMessage);
	}
}
