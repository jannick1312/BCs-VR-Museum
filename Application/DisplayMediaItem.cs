using Core;
namespace Application;

public class DisplayMediaItem(MediaType mediaType, byte[] bytes)
{
    public MediaType MediaType { get; } = mediaType;
    public byte[] Bytes { get; } = bytes;
}