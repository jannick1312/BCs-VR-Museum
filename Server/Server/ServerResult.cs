namespace Server;

public class ServerResult
{
    public bool Success { get; }
    public string LocalImagePath { get; }
    public string RemoteImageUrl { get; }
    public string ErrorMessage { get; }

    private ServerResult(bool success, string localImagePath, string remoteImageUrl, string errorMessage)
    {
        Success = success;
        LocalImagePath = localImagePath;
        RemoteImageUrl = remoteImageUrl;
        ErrorMessage = errorMessage;
    }

    public static ServerResult FromImage(string localImagePath, string remoteImageUrl)
    {
        return new ServerResult(true, localImagePath, remoteImageUrl, "");
    }

    public static ServerResult FromError(string errorMessage)
    {
        return new ServerResult(false, "", "", errorMessage);
    }
}