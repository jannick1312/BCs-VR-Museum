namespace Models;

public class DisplayMediaItem(MediaType mediaType, byte[] bytes, string path, string name)
{
    public MediaType MediaType { get; } = mediaType;
    public byte[] Bytes { get; } = bytes;
    public string Path { get; } = path;
    public string Name { get; } = name;
}