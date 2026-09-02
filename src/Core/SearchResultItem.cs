using Models;

namespace Core;

/// <summary>
/// Represents a media item found by a search.
/// </summary>
/// <param name="vector">The feature vector for the result.</param>
/// <param name="mediaType">The type of media.</param>
/// <param name="localPath">The expected local path of the media file.</param>
/// <param name="remoteUrl">The web address used to download the media file.</param>
/// <param name="startTimeSeconds">The optional playback start time in seconds.</param>
/// <param name="metadata">The metadata for the media.</param>
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
