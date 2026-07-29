using Models;

namespace Core;

public class SearchResultItem(IReadOnlyList<double> vector, MediaType mediaType, string localPath, string remoteUrl, int? startTimeSeconds, MediaMetadata metadata)
{
	public IReadOnlyList<double> Vector { get; } = vector;
	public MediaType MediaType { get; } = mediaType;
	public string LocalPath { get; } = localPath;
	public string RemoteUrl { get; } = remoteUrl;
	public string Name { get; } = Path.GetFileName(localPath);
	public int? StartTimeSeconds { get; } = startTimeSeconds;
	public MediaMetadata Metadata { get; } = metadata;
}
