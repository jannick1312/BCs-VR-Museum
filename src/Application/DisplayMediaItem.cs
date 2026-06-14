using Core;
namespace Application;

public class DisplayMediaItem(MediaType mediaType, byte[] bytes, string name)
{
    public MediaType MediaType { get; } = mediaType;
    public byte[] Bytes { get; } = bytes;
    public string Name { get; } = name;
}