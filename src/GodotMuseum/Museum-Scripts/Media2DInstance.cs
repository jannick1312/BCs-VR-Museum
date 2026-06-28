using Godot;

namespace BCSVRMuseum.Museum_Scripts;

public class Media2DInstance
{
    private readonly Node3D _place;
    private readonly Rect2 _slot;
    private readonly float _cellPadding;
    private readonly FrameMaker _frameMaker;
    private VideoStreamPlayer _videoPlayer;

    public Node3D Item { get; }

    private MeshInstance3D DisplaySurface { get; }

    private Media2DInstance(Node3D item, MeshInstance3D displaySurface, Node3D place, Rect2 slot, float cellPadding)
    {
        Item = item;
        DisplaySurface = displaySurface;
        _place = place;
        _slot = slot;
        _cellPadding = cellPadding;
        _frameMaker = item.GetNode<FrameMaker>("FrameMaker");
    }

    public static Media2DInstance Create(Node3D template, Node3D outputRoot, string groupName, float cellPadding, Node3D place, Rect2 slot)
    {
        var item = (Node3D)template.Duplicate();
        item.AddToGroup(groupName);
        outputRoot.AddChild(item);
        SetTreeActive(item, true);

        return new Media2DInstance(item, item.GetNode<MeshInstance3D>("Picture"), place, slot, cellPadding);
    }

    public void ApplyTexture(Texture2D texture)
    {
        DisplaySurface.MaterialOverride = new StandardMaterial3D{CullMode = BaseMaterial3D.CullModeEnum.Disabled, AlbedoTexture = texture};
        ResizeToAspect((float)texture.GetWidth() / texture.GetHeight());
    }

    private void ResizeToAspect(float aspect)
    {
        var maxWidth = Mathf.Max(0.1f, _slot.Size.X - _cellPadding);
        var maxHeight = Mathf.Max(0.1f, _slot.Size.Y - _cellPadding);

        var width = maxWidth;
        var height = width / aspect;

        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspect;
        }

        var x = _slot.Position.X + _slot.Size.X / 2.0f;
        var y = _slot.Position.Y + _slot.Size.Y / 2.0f;

        Item.GlobalTransform = new Transform3D(_place.GlobalTransform.Basis.Orthonormalized(), _place.ToGlobal(new Vector3(x, y, 0)));
        DisplaySurface.Scale = new Vector3(width, height, 1.0f);
        _frameMaker.UpdateFrame(DisplaySurface, width, height);
    }

    public void AttachVideo(VideoStreamPlayer player)
    {
        _videoPlayer = player;
        Item.AddChild(player);

        player.Finished += StartVideoFromBeginning;
    }

    private void ResetVideo()
    {
        if (_videoPlayer == null || !GodotObject.IsInstanceValid(_videoPlayer))
            return;

        _videoPlayer.Paused = false;
        _videoPlayer.Stop();
        _videoPlayer.StreamPosition = 0.0;
    }

    public void UpdateVideoForDistance(Node3D camera, float activeDistance)
    {
        if (_videoPlayer == null || camera == null ||
            !GodotObject.IsInstanceValid(_videoPlayer) ||
            !GodotObject.IsInstanceValid(camera) ||
            !GodotObject.IsInstanceValid(Item))
            return;

        var isInRange = Item.GlobalPosition.DistanceTo(camera.GlobalPosition) <= activeDistance;

        if (!isInRange)
        {
            if (_videoPlayer.IsPlaying())
                ResetVideo();
            return;
        }

        if (!_videoPlayer.IsPlaying() || _videoPlayer.Paused)
            StartVideoFromBeginning();
    }

    private void StartVideoFromBeginning()
    {
        if (_videoPlayer == null || !GodotObject.IsInstanceValid(_videoPlayer))
            return;

        _videoPlayer.Paused = false;
        _videoPlayer.Stop();
        _videoPlayer.StreamPosition = 0.0;
        _videoPlayer.Play();
    }

    private static void SetTreeActive(Node node, bool active)
    {
        if (node is Node3D node3D)
            node3D.Visible = active;

        if (node is CollisionShape3D collisionShape)
            collisionShape.SetDeferred(CollisionShape3D.PropertyName.Disabled, !active);

        foreach (var child in node.GetChildren())
            SetTreeActive(child, active);
    }
}