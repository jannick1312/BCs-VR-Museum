using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Media2D;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Video;

public static class Video2DMediaRenderer
{
	public static async Task<VideoPlaybackController> Render(Media2DDisplayInstance instance, string path)
	{
		var player = new VideoStreamPlayer { Stream = new VideoStreamTheora { File = path }, Autoplay = false, Loop = false };

		instance.Item.AddChild(player);

		await instance.Item.ToSignal(instance.Item.GetTree(), SceneTree.SignalName.ProcessFrame);
		instance.ShowTexture(player.GetVideoTexture());
		return new VideoPlaybackController(instance.Item, player);
	}
}
