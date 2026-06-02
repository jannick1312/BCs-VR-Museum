namespace Core;

public class MediaContent
{
    public bool Success { get; }
    public byte[] Bytes { get; }
    public string ErrorMessage { get; }

    private MediaContent(bool success, byte[] bytes, string errorMessage)
    {
        Success = success;
        Bytes = bytes;
        ErrorMessage = errorMessage;
    }

    public static MediaContent FromBytes(byte[] bytes)
    {
        return new MediaContent(true, bytes, "");
    }

    public static MediaContent Failure(string errorMessage)
    {
        return new MediaContent(false, [], errorMessage);
    }
}