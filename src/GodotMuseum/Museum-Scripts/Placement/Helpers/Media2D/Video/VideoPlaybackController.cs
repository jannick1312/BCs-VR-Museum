using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Video;

public sealed class VideoPlaybackController
{
	private readonly Node3D _item;
	private readonly Node3D _playIndicator;
	private readonly double _startPositionSeconds;
	private readonly VideoStreamPlayer _videoPlayer;
	private bool _isPlayingForViewer;

	public VideoPlaybackController(Node3D item, VideoStreamPlayer videoPlayer, Node3D playIndicator, double startPositionSeconds)
	{
		_item = item;
		_videoPlayer = videoPlayer;
		_playIndicator = playIndicator;
		_startPositionSeconds = startPositionSeconds;
		_videoPlayer.Finished += StartFromKeyframe;
	}

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

	private void Reset()
	{
		_isPlayingForViewer = false;
		_videoPlayer.Paused = true;
		_playIndicator.Visible = true;
	}

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
