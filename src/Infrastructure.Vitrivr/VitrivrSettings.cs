namespace Infrastructure.Vitrivr;

public sealed class VitrivrSettings(
    string currentIp,
    string mediaFolderPath)
{
    private string CurrentIp { get; } = currentIp;
    public string MediaFolderPath { get; } = mediaFolderPath;

    public string QueryUrl => $"http://{CurrentIp}:7070/api/sandbox/query";
    public string MediaBaseUrl => $"http://{CurrentIp}:9090/";
}