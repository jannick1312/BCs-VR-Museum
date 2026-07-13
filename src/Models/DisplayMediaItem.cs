namespace Models;

public class DisplayMediaItem(IReadOnlyList<double> vector, MediaType mediaType, byte[] bytes, string path, string name)
{
	public IReadOnlyList<double> Vector { get; } = vector;
	public MediaType MediaType { get; } = mediaType;
	public byte[] Bytes { get; } = bytes;
	public string Path { get; } = path;
	public string Name { get; } = name;
}
