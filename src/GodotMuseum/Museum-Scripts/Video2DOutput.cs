using Godot;
using System.IO;
using System.Threading.Tasks;

namespace BCSVRMuseum.Museum_Scripts;

public static class Video2DOutput
{
    public static async Task Set(Media2DInstance instance, byte[] bytes, string path, string name)
    {
        var player = new VideoStreamPlayer{Stream = new VideoStreamTheora { File = File.Exists(path) ? path : SaveVideo(bytes, name) }, Autoplay = false, Loop = false};

        instance.AttachVideo(player);

        await instance.Item.ToSignal(instance.Item.GetTree(), SceneTree.SignalName.ProcessFrame);
        instance.ApplyTexture(player.GetVideoTexture());
    }

    private static string SaveVideo(byte[] bytes, string name)
    {
        var directory = ProjectSettings.GlobalizePath("user://search-videos");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }
}