namespace Models;

public class DisplayMediaItem(IReadOnlyList<double> vector, MediaType mediaType, string path, string name)
{
	public IReadOnlyList<double> Vector { get; } = vector;
	public MediaType MediaType { get; } = mediaType;
	public string Path { get; } = path;
	public string Name { get; } = name;
}
