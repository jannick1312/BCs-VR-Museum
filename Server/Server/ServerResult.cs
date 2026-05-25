namespace Server;

public class ServerResult
{
    public bool Success { get; }
    public string ImageUrl { get; }
    public string LocalImagePath { get; }
    public string ErrorMessage { get; }

    public bool IsUrlResult => !string.IsNullOrWhiteSpace(ImageUrl);
    public bool IsLocalPathResult => !string.IsNullOrWhiteSpace(LocalImagePath);

    private ServerResult(bool success, string imageUrl, string localImagePath, string errorMessage)
    {
        Success = success;
        ImageUrl = imageUrl;
        LocalImagePath = localImagePath;
        ErrorMessage = errorMessage;
    }

    public static ServerResult FromUrl(string imageUrl)
    {
        return new ServerResult(true, imageUrl, "", "");
    }

    public static ServerResult FromLocalPath(string localImagePath)
    {
        return new ServerResult(true, "", localImagePath, "");
    }

    public static ServerResult Fail(string errorMessage)
    {
        return new ServerResult(false, "", "", errorMessage);
    }
}