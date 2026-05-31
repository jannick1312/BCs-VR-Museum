using Core;

namespace Application;

public class DisplayMediaResult
{
    public bool Success { get; }
    public MediaType MediaType { get; }
    public byte[] Bytes { get; }
    public string FileName { get; }
    public string Source { get; }
    public string ErrorMessage { get; }

    private DisplayMediaResult(bool success, MediaType mediaType, byte[] bytes, string fileName, string source, string errorMessage)
    {
        Success = success;
        MediaType = mediaType;
        Bytes = bytes;
        FileName = fileName;
        Source = source;
        ErrorMessage = errorMessage;
    }

    public static DisplayMediaResult FromMedia(MediaType mediaType, byte[] bytes, string fileName, string source)
    {
        return new DisplayMediaResult(true, mediaType, bytes, fileName, source, "");
    }

    public static DisplayMediaResult Failure(string errorMessage)
    {
        return new DisplayMediaResult(false, MediaType.Unknown, [], "", "", errorMessage);
    }
}