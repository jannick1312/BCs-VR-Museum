using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Video;

/// <summary>
/// Starts and resets a video based on the viewer's distance.
/// </summary>
public sealed class VideoPlaybackController
{
	private readonly Node3D _item;
	private readonly Node3D _playIndicator;
	private readonly double _startPositionSeconds;
	private readonly VideoStreamPlayer _videoPlayer;
	private bool _isPlayingForViewer;

	/// <summary>
	/// Sets up playback for a video display.
	/// </summary>
	/// <param name="item">The positioned video display.</param>
	/// <param name="videoPlayer">The player rendering the video.</param>
	/// <param name="playIndicator">The indicator shown while playback is paused.</param>
	/// <param name="startPositionSeconds">The position used whenever playback restarts.</param>
	public VideoPlaybackController(Node3D item, VideoStreamPlayer videoPlayer, Node3D playIndicator, double startPositionSeconds)
	{
		_item = item;
		_videoPlayer = videoPlayer;
		_playIndicator = playIndicator;
		_startPositionSeconds = startPositionSeconds;
		_videoPlayer.Finished += StartFromKeyframe;
	}

	/// <summary>
	/// Starts playback within viewing distance and resets it outside that distance.
	/// </summary>
	/// <param name="camera">The viewer camera.</param>
	/// <param name="activeDistance">The maximum distance for active playback.</param>
	public void UpdateForDistance(Node3D camera, float activeDistance)
	{
		var isInRange = _item.GlobalPosition.DistanceTo(camera.GlobalPosition) <= activeDistance;

		if (!isInRange)
		{
			if (_isPlayingForViewer)
				Reset();
			return;
		}

		if (!_isPlayingForViewer)
			StartFromKeyframe();
	}

	/// <summary>
	/// Pauses the video and restores its play indicator.
	/// </summary>
	private void Reset()
	{
		_isPlayingForViewer = false;
		_videoPlayer.Paused = true;
		_playIndicator.Visible = true;
	}

	/// <summary>
	/// Restarts the video at its set start time.
	/// </summary>
	private void StartFromKeyframe()
	{
		_isPlayingForViewer = true;
		_playIndicator.Visible = false;
		_videoPlayer.Paused = false;
		_videoPlayer.Stop();
		_videoPlayer.Play();
		_videoPlayer.StreamPosition = _startPositionSeconds;
	}
}
