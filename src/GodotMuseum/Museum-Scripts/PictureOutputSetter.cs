using Godot;
using Infrastructure.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BCSVRMuseum.Museum_Scripts;

public partial class PictureOutputSetter : Node
{
    [Export] public NodePath OutputInstancePath;
    [Export] public NodePath OutputPlacesPath;

    [Export] public float CellPadding;

    private Node3D _outputRoot;
    private Node3D _outputTemplate;
    private Node _outputPlacesRoot;
    private readonly RandomNumberGenerator _rng = new();
    private static readonly EventLogger Logger = new(nameof(PictureOutputSetter));

    public override void _Ready()
    {
        _outputRoot = GetNodeOrNull<Node3D>(OutputInstancePath);
        _outputPlacesRoot = GetNodeOrNull(OutputPlacesPath);

        if (_outputRoot == null)
        {
            Logger.Error("Picture output root is missing.");
            return;
        }

        if (_outputPlacesRoot == null)
        {
            Logger.Error("Picture output places root is missing.");
            return;
        }

        _rng.Randomize();
        ClearGeneratedPictures();
        _outputTemplate = _outputRoot.Duplicate() as Node3D;

        if (_outputTemplate == null)
            Logger.Error("Picture output template could not be duplicated as Node3D.");
    }

    public async Task SetOutputImagesFromBytes(IReadOnlyList<byte[]> imageBytes)
    {
        ClearGeneratedPictures();

        var places = GetOutputPlaces();

        if (imageBytes.Count > places.Count)
            Logger.Warning($"Only {places.Count} of {imageBytes.Count} images can be placed.");

        var availableBytes = new List<byte[]>(imageBytes);
        for (var i = availableBytes.Count - 1; i > 0; i--)
        {
            var j = _rng.RandiRange(0, i);
            (availableBytes[i], availableBytes[j]) = (availableBytes[j], availableBytes[i]);
        }

        var nextImageIndex = 0;

        foreach (var place in places)
        {
            if (nextImageIndex >= availableBytes.Count)
                break;

            var randomCount = _rng.RandiRange(1, Mathf.Min(4, availableBytes.Count - nextImageIndex));
            var slots = WallImageLayout.CreateCenteredHorizontalSlots(randomCount, GetPlaceWidth(place), GetPlaceHeight(place));

            for (var i = 0; i < randomCount; i++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

                var texture = LoadTexture(availableBytes[nextImageIndex]);
                nextImageIndex++;

                if (texture == null)
                    continue;

                CreatePictureInstance(texture, place, slots[i]);
            }
        }
    }

    private void ClearGeneratedPictures()
    {
        foreach (var child in _outputRoot.GetChildren())
        {
            if (child != null && child.IsInGroup("GeneratedOutputPicture"))
                child.QueueFree();
        }
    }

    private static ImageTexture LoadTexture(byte[] bytes)
    {
        var image = new Image();

        var loadError = image.LoadJpgFromBuffer(bytes);

        if (loadError != Error.Ok)
            loadError = image.LoadPngFromBuffer(bytes);

        if (loadError != Error.Ok)
            loadError = image.LoadWebpFromBuffer(bytes);

        if (loadError == Error.Ok && !image.IsEmpty()) return ImageTexture.CreateFromImage(image);
        Logger.Error($"Could not load image from bytes. Error: {loadError}");
        return null;
    }

    private List<Node3D> GetOutputPlaces()
    {
        var result = new List<Node3D>();

        foreach (var child in _outputPlacesRoot.GetChildren())
        {
            if (child is Node3D place)
                result.Add(place);
        }

        result.Sort((a, b) => a.GetIndex().CompareTo(b.GetIndex()));
        return result;
    }

    private static float GetPlaceWidth(Node3D place)
    {
        var mesh = (MeshInstance3D)place;
        return Mathf.Max(0.1f, mesh.GetAabb().Size.X * mesh.Scale.X);
    }

    private static float GetPlaceHeight(Node3D place)
    {
        var mesh = (MeshInstance3D)place;
        return Mathf.Max(0.1f, mesh.GetAabb().Size.Y * mesh.Scale.Y);
    }

    private void CreatePictureInstance(ImageTexture texture, Node3D place, Rect2 slot)
    {
        var item = _outputTemplate.Duplicate() as Node3D;

        item.AddToGroup("GeneratedOutputPicture");

        _outputRoot.AddChild(item);
        SetTreeActive(item, true);

        var picture = item.GetNode<MeshInstance3D>("Picture");

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
        var y = slot.Position.Y + slot.Size.Y / 2.0f;

        item.GlobalTransform = new Transform3D(place.GlobalTransform.Basis.Orthonormalized(), place.ToGlobal(new Vector3(x, y, 0)));

        picture.Scale = new Vector3(imageWidth, imageHeight, 1.0f);

        var frameMaker = item.GetNodeOrNull<FrameMaker>("FrameMaker");

        if (frameMaker == null)
        {
            Logger.Warning(" Creating FrameMaker dynamically.");
            frameMaker = new FrameMaker();
            item.AddChild(frameMaker);
        }

        frameMaker.UpdateFrame(picture, imageWidth, imageHeight);
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