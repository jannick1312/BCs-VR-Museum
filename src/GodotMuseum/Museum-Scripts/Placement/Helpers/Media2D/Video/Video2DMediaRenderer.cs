using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Media2D;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Video;

public static class Video2DMediaRenderer
{
	public static async Task<VideoPlaybackController> Render(Media2DDisplayInstance instance, string path)
	{
		var player = new VideoStreamPlayer { Stream = new VideoStreamTheora { File = path }, Autoplay = false, Loop = false };
		player.VolumeDb = -80.0f;

		instance.Item.AddChild(player);

		await instance.Item.ToSignal(instance.Item.GetTree(), SceneTree.SignalName.ProcessFrame);
		player.Play();
		await instance.Item.ToSignal(instance.Item.GetTree(), SceneTree.SignalName.ProcessFrame);
		await instance.Item.ToSignal(instance.Item.GetTree(), SceneTree.SignalName.ProcessFrame);
		player.Paused = true;
		player.VolumeDb = 0.0f;

		instance.ShowTexture(player.GetVideoTexture());
		var playIndicator = instance.Item.GetNode<Label3D>("Play");
		playIndicator.Visible = true;
		return new VideoPlaybackController(instance.Item, player, playIndicator);
	}
}
