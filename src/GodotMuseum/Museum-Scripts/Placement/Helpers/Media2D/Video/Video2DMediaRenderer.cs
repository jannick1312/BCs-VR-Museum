using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Media2D;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Video;

/// <summary>
/// Creates video playback for wall displays.
/// </summary>
public static class Video2DMediaRenderer
{
	/// <summary>
	/// Creates a paused video texture at its set start time.
	/// </summary>
	/// <param name="instance">The display instance receiving the video texture.</param>
	/// <param name="path">The video file path.</param>
	/// <param name="startTimeSeconds">The optional playback start time in seconds.</param>
	/// <returns>A task containing the controller used to update playback.</returns>
	public static async Task<VideoPlaybackController> Render(Media2DDisplayInstance instance, string path, int? startTimeSeconds)
	{
		var player = new VideoStreamPlayer { Stream = new VideoStreamTheora { File = path }, Autoplay = false, Loop = false };
		player.VolumeDb = -80.0f;
		var startPositionSeconds = startTimeSeconds is > 0 ? startTimeSeconds.Value : 0.0;

		instance.Item.AddChild(player);

		await instance.Item.ToSignal(instance.Item.GetTree(), SceneTree.SignalName.ProcessFrame);
		player.Play();
		player.StreamPosition = startPositionSeconds;
		await instance.Item.ToSignal(instance.Item.GetTree(), SceneTree.SignalName.ProcessFrame);
		await instance.Item.ToSignal(instance.Item.GetTree(), SceneTree.SignalName.ProcessFrame);
		player.Paused = true;
		player.VolumeDb = 0.0f;

		instance.ShowTexture(player.GetVideoTexture());
		var playIndicator = instance.Item.GetNode<Label3D>("Play");
		playIndicator.Visible = true;
		return new VideoPlaybackController(instance.Item, player, playIndicator, startPositionSeconds);
	}
}
