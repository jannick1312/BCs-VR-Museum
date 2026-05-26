namespace Server;

public class ServerResult
{
    public bool Success { get; }
    public string Filename { get; }
    public string LocalImagePath { get; }
    public string RemoteImageUrl { get; }
    public string ErrorMessage { get; }

    private ServerResult(bool success, string filename, string localImagePath, string remoteImageUrl, string errorMessage)
    {
        Success = success;
        Filename = filename;
        LocalImagePath = localImagePath;
        RemoteImageUrl = remoteImageUrl;
        ErrorMessage = errorMessage;
    }

    public static ServerResult FromImage(string filename, string localImagePath, string remoteImageUrl)
    {
        return new ServerResult(true, filename, localImagePath, remoteImageUrl, "");
    }

    public static ServerResult Fail(string errorMessage)
    {
        return new ServerResult(false, "", "", "", errorMessage);
    }
}