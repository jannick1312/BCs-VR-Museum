using Godot;

namespace BCSVRMuseum.Museum_Scripts;

public static class Image2DOutput
{
    public static void Set(Media2DInstance instance, byte[] bytes)
    {
        var texture = LoadTexture(bytes);
        instance.ApplyTexture(texture);
    }

    private static ImageTexture LoadTexture(byte[] bytes)
    {
        var image = new Image();
        image.LoadJpgFromBuffer(bytes);
        return ImageTexture.CreateFromImage(image);
    }
}