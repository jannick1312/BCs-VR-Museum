namespace Core;

public class MediaContent
{
	private MediaContent(bool success, byte[] bytes, string path, string errorMessage)
	{
		Success = success;
		Bytes = bytes;
		Path = path;
		ErrorMessage = errorMessage;
	}

	public bool Success { get; }
	public byte[] Bytes { get; }
	public string Path { get; }
	public string ErrorMessage { get; }

	public static MediaContent FromBytes(byte[] bytes)
	{
		return new MediaContent(true, bytes, "", "");
	}

	public static MediaContent FromPath(string path)
	{
		return new MediaContent(true, [], path, "");
	}

	public static MediaContent Failure(string errorMessage)
	{
		return new MediaContent(false, [], "", errorMessage);
	}
}
