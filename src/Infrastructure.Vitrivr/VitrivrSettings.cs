namespace Infrastructure.Vitrivr;

/// <summary>
/// Provides the server and media locations used for Vitrivr searches.
/// </summary>
/// <param name="currentIp">The network address of the Vitrivr server.</param>
/// <param name="mediaFolderPath">The local media folder used by the application.</param>
public sealed class VitrivrSettings(string currentIp, string mediaFolderPath)
{
	private string CurrentIp { get; } = currentIp;
	public string MediaFolderPath { get; } = mediaFolderPath;

	public string QueryUrl => $"http://{CurrentIp}:7070/api/sandbox/query";
	public string SchemaListUrl => $"http://{CurrentIp}:7070/api/schema/list";
	public string MediaBaseUrl => $"http://{CurrentIp}:9090/";
}
