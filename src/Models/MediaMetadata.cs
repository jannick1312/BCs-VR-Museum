namespace Models;

/// <summary>
/// Represents metadata for a media item.
/// </summary>
/// <param name="id">The source identifier of the media item.</param>
public class MediaMetadata(string? id)
{
	public string? Id { get; } = id;
}
