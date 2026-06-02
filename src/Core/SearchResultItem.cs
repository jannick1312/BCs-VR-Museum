namespace Core;

public class SearchResultItem(MediaType mediaType, string localPath, string remoteUrl)
{
    public MediaType MediaType { get; } = mediaType;
    public string LocalPath { get; } = localPath;
    public string RemoteUrl { get; } = remoteUrl;
}