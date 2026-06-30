using System.IO;
using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Media2D;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Video;

public static class Video2DMediaRenderer
{
	public static async Task<VideoPlaybackController> Render(Media2DDisplayInstance instance, byte[] bytes, string path, string name)
	{
		var player = new VideoStreamPlayer
		{
			Stream = new VideoStreamTheora { File = File.Exists(path) ? path : SaveVideo(bytes, name) },
			Autoplay = false,
			Loop = false
		};

		instance.Item.AddChild(player);

		await instance.Item.ToSignal(instance.Item.GetTree(), SceneTree.SignalName.ProcessFrame);
		instance.ShowTexture(player.GetVideoTexture());
		return new VideoPlaybackController(instance.Item, player);
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