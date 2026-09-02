namespace Models;

/// <summary>
/// Represents a loaded media item that is ready to show.
/// </summary>
/// <param name="vector">The feature vector for the media.</param>
/// <param name="mediaType">The type of media.</param>
/// <param name="path">The path to the loaded media file.</param>
/// <param name="name">The display name of the media.</param>
/// <param name="startTimeSeconds">The optional playback start time in seconds.</param>
/// <param name="metadata">The metadata for the media.</param>
public class DisplayMediaItem(IReadOnlyList<double> vector, MediaType mediaType, string path, string name, int? startTimeSeconds, MediaMetadata metadata)
{
	public IReadOnlyList<double> Vector { get; } = vector;
	public MediaType MediaType { get; } = mediaType;
	public string Path { get; } = path;
	public string Name { get; } = name;
	public int? StartTimeSeconds { get; } = startTimeSeconds;
	public MediaMetadata Metadata { get; } = metadata;
}
