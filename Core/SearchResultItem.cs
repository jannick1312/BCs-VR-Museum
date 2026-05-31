namespace Core;

public class SearchResultItem(MediaType mediaType, string fileName, string localPath, string remoteUrl)
{
    public MediaType MediaType { get; } = mediaType;
    public string FileName { get; } = fileName;
    public string LocalPath { get; } = localPath;
    public string RemoteUrl { get; } = remoteUrl;
}