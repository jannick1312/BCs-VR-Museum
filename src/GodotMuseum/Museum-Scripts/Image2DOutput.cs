using Godot;

namespace BCSVRMuseum.Museum_Scripts;

public static class Image2DOutput
{
    public static void Set(Media2DInstance instance, byte[] bytes, string path)
    {
        var texture = System.IO.File.Exists(path) ? LoadTexture(path) : LoadTexture(bytes);
        instance.ApplyTexture(texture);
    }

    private static ImageTexture LoadTexture(string path)
    {
        var image = new Image();
        image.Load(path);
        return ImageTexture.CreateFromImage(image);
    }

    private static ImageTexture LoadTexture(byte[] bytes)
    {
        var image = new Image();
        image.LoadJpgFromBuffer(bytes);
        return ImageTexture.CreateFromImage(image);
    }
}