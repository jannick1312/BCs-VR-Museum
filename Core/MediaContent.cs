namespace Core;

public class MediaContent
{
    public bool Success { get; }
    public byte[] Bytes { get; }
    public string Source { get; }
    public string ErrorMessage { get; }

    private MediaContent(bool success, byte[] bytes, string source, string errorMessage)
    {
        Success = success;
        Bytes = bytes;
        Source = source;
        ErrorMessage = errorMessage;
    }

    public static MediaContent FromBytes(byte[] bytes, string source)
    {
        return new MediaContent(true, bytes, source, "");
    }

    public static MediaContent Failure(string errorMessage)
    {
        return new MediaContent(false, [], "", errorMessage);
    }
}