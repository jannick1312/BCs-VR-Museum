using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Video;

public sealed class VideoPlaybackController
{
    private readonly Node3D _item;
    private readonly VideoStreamPlayer _videoPlayer;

    public VideoPlaybackController(Node3D item, VideoStreamPlayer videoPlayer)
    {
        _item = item;
        _videoPlayer = videoPlayer;
        _videoPlayer.Finished += StartFromBeginning;
    }

    public void UpdateForDistance(Node3D camera, float activeDistance)
    {
        if (camera == null ||
            !GodotObject.IsInstanceValid(_videoPlayer) ||
            !GodotObject.IsInstanceValid(camera) ||
            !GodotObject.IsInstanceValid(_item))
            return;

        var isInRange = _item.GlobalPosition.DistanceTo(camera.GlobalPosition) <= activeDistance;

        if (!isInRange)
        {
            if (_videoPlayer.IsPlaying())
                Reset();
            return;
        }

        if (!_videoPlayer.IsPlaying() || _videoPlayer.Paused)
            StartFromBeginning();
    }

    private void Reset()
    {
        if (!GodotObject.IsInstanceValid(_videoPlayer))
            return;

        _videoPlayer.Paused = false;
        _videoPlayer.Stop();
        _videoPlayer.StreamPosition = 0.0;
    }

    private void StartFromBeginning()
    {
        if (!GodotObject.IsInstanceValid(_videoPlayer))
            return;

        _videoPlayer.Paused = false;
        _videoPlayer.Stop();
        _videoPlayer.StreamPosition = 0.0;
        _videoPlayer.Play();
    }
}