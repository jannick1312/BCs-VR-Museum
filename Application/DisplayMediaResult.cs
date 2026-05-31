using Core;

namespace Application;

public class DisplayMediaResult
{
    public bool Success { get; }
    public MediaType MediaType { get; }
    public byte[] Bytes { get; }
    public string ErrorMessage { get; }

    private DisplayMediaResult( bool success, MediaType mediaType, byte[] bytes, string errorMessage)
    {
        Success = success;
        MediaType = mediaType;
        Bytes = bytes;
        ErrorMessage = errorMessage;
    }

    public static DisplayMediaResult FromMedia(MediaType mediaType, byte[] bytes)
    {
        return new DisplayMediaResult(true, mediaType, bytes, "");
    }

    public static DisplayMediaResult Failure(string errorMessage)
    {
        return new DisplayMediaResult(false, MediaType.Unknown, [], errorMessage);
    }
}