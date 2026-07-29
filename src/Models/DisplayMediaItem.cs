namespace Models;

public class DisplayMediaItem(IReadOnlyList<double> vector, MediaType mediaType, string path, string name, int? startTimeSeconds, MediaMetadata metadata)
{
	public IReadOnlyList<double> Vector { get; } = vector;
	public MediaType MediaType { get; } = mediaType;
	public string Path { get; } = path;
	public string Name { get; } = name;
	public int? StartTimeSeconds { get; } = startTimeSeconds;
	public MediaMetadata Metadata { get; } = metadata;
}
