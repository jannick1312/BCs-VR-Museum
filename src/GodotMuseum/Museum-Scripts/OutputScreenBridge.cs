using Godot;
using System.Collections.Generic;
using System.Linq;

namespace BCSVRMuseum.Museum_Scripts;

public partial class OutputScreenBridge : Node
{
    [Export] public NodePath OutputFramePath;
    [Export] public string OutputScenePath = "res://Museum/Output.tscn";

    [Export] public float WallCenterX;
    [Export] public float WallZ = -4.99f;

    [Export] public float WallWidth = 10.0f;
    [Export] public float WallBottomY = 0.05f;
    [Export] public float WallHeight = 2.85f;

    [Export] public float CellPadding = 0.40f;

    private Node3D _outputRoot;
    private PackedScene _outputScene;
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _outputRoot = GetNodeOrNull<Node3D>(OutputFramePath);
        _outputScene = GD.Load<PackedScene>(OutputScenePath);
        _rng.Randomize();
        ClearGeneratedPictures();
    }

    public void SetOutputImagesFromBytes(IReadOnlyList<byte[]> imageBytes)
    {
        ClearGeneratedPictures();

        var randomCount = _rng.RandiRange(1, Mathf.Min(4, imageBytes.Count));

        var selectedBytes = imageBytes.OrderBy(_ => _rng.Randf()).Take(randomCount).ToList();

        var slots = WallImageLayout.CreateHorizontalSlots(selectedBytes.Count, WallCenterX, WallWidth, WallBottomY, WallHeight);

        for (var i = 0; i < selectedBytes.Count; i++)
        {
            var texture = LoadTexture(selectedBytes[i]);

            CreatePictureInstance(texture, slots[i]);
        }
    }

    private void ClearGeneratedPictures()
    {
        foreach (var child in _outputRoot.GetChildren())
        {
            if (child is Node node && node.IsInGroup("GeneratedOutputPicture"))
                node.QueueFree();
        }
    }

    private ImageTexture LoadTexture(byte[] bytes)
    {
        var image = new Image();

        var loadError = image.LoadJpgFromBuffer(bytes);

        if (loadError != Error.Ok)
            loadError = image.LoadPngFromBuffer(bytes);

        if (loadError != Error.Ok)
            loadError = image.LoadWebpFromBuffer(bytes);

        if (loadError != Error.Ok || image.IsEmpty())
        {
            GD.PrintErr("Could not load image from bytes. Error: " + loadError);
            return null;
        }

        return ImageTexture.CreateFromImage(image);
    }

    private async void CreatePictureInstance(ImageTexture texture, Rect2 slot)
    {
        var item = _outputScene.Instantiate<Node3D>();

        item.AddToGroup("GeneratedOutputPicture");

        _outputRoot.AddChild(item);

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        item.Visible = true;

        var picture = item.GetNodeOrNull<MeshInstance3D>("Picture");

        var material = new StandardMaterial3D { CullMode = BaseMaterial3D.CullModeEnum.Disabled, AlbedoTexture = texture };

        picture.MaterialOverride = material;

        var aspect = (float)texture.GetWidth() / texture.GetHeight();

        var maxWidth = Mathf.Max(0.1f, slot.Size.X - CellPadding);
        var maxHeight = Mathf.Max(0.1f, slot.Size.Y - CellPadding);

        var imageWidth = maxWidth;
        var imageHeight = imageWidth / aspect;

        if (imageHeight > maxHeight)
        {
            imageHeight = maxHeight;
            imageWidth = imageHeight * aspect;
        }

        var x = slot.Position.X + slot.Size.X / 2.0f;
        var y = 1.55f;
        
        item.GlobalPosition = new Vector3(x, y, WallZ);
        item.GlobalRotation = Vector3.Zero;

        picture.Scale = new Vector3(imageWidth, imageHeight, 1.0f);

        var frameMaker = item.GetNodeOrNull<FrameMaker>("FrameMaker");

        if (frameMaker == null)
        {
            frameMaker = new FrameMaker();
            item.AddChild(frameMaker);
        }

        frameMaker.UpdateFrame(picture, imageWidth, imageHeight);
    }
}