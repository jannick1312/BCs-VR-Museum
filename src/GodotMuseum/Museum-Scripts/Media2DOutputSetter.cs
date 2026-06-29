using Godot;
using Logger;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BCSVRMuseum.Museum_Scripts;

public partial class Media2DOutputSetter : Node
{
    [Export] public NodePath OutputInstancePath;
    [Export] public NodePath OutputPlacesPath;

    [Export] public float CellPadding;
    [Export] public float VideoResetDistance = 2.0f;

    private Node3D _outputRoot;
    private Node3D _outputTemplate;
    private Node _outputPlacesRoot;
    private Node3D _playerCamera;
    private readonly RandomNumberGenerator _rng = new();
    private readonly List<Media2DInstance> _mediaInstances = [];
    private static readonly EventLogger Logger = new(nameof(Media2DOutputSetter));
    private const string GeneratedMediaGroup = "GeneratedOutput2DMedia";

    public override void _Ready()
    {
        _outputRoot = GetNodeOrNull<Node3D>(OutputInstancePath);
        _outputPlacesRoot = GetNodeOrNull(OutputPlacesPath);
        _playerCamera = GetTree().Root.FindChild("XRCamera3D", true, false) as Node3D;

        if (_outputRoot == null)
        {
            Logger.Error("2D media output root is missing.");
            return;
        }

        if (_outputPlacesRoot == null)
        {
            Logger.Error("2D media output places root is missing.");
            return;
        }

        _rng.Randomize();
        ClearGenerated2DMedia();
        _outputTemplate = _outputRoot.Duplicate() as Node3D;

        if (_outputTemplate == null)
            Logger.Error("2D media output template could not be duplicated as Node3D.");
    }

    public async Task SetOutput2DMedia(IReadOnlyList<byte[]> mediaBytes, IReadOnlyList<string> mediaPaths, IReadOnlyList<string> mediaNames, IReadOnlyList<bool> mediaIsVideo)
    {
        ClearGenerated2DMedia();

        var places = GetOutputPlaces();

        var nextMediaIndex = 0;
        var placedMediaCount = 0;

        foreach (var place in places)
        {
            if (nextMediaIndex >= mediaBytes.Count)
                break;

            var randomCount = _rng.RandiRange(1, Mathf.Min(4, mediaBytes.Count - nextMediaIndex));
            var slots = WallImageLayout.CreateCenteredHorizontalSlots(randomCount, GetPlaceWidth(place), GetPlaceHeight(place));

            for (var i = 0; i < randomCount; i++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

                var bytes = mediaBytes[nextMediaIndex];
                var path = mediaPaths[nextMediaIndex];
                var name = mediaNames[nextMediaIndex];
                var isVideo = mediaIsVideo[nextMediaIndex];
                nextMediaIndex++;

                if (await Create2DMediaInstance(bytes, path, name, isVideo, place, slots[i]))
                    placedMediaCount++;
            }
        }

        if (placedMediaCount < mediaBytes.Count)
            Logger.Warning($"Placed {placedMediaCount} of {mediaBytes.Count} images/videos.");
        else
            Logger.Info($"Placed all {placedMediaCount} images/videos.");
    }

    public override void _Process(double delta)
    {
        foreach (var instance in _mediaInstances)
            instance.UpdateVideoForDistance(_playerCamera, VideoResetDistance);
    }

    private void ClearGenerated2DMedia()
    {
        _mediaInstances.Clear();

        foreach (var child in _outputRoot.GetChildren())
        {
            if (child != null && child.IsInGroup(GeneratedMediaGroup))
                child.QueueFree();
        }
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

    private async Task<bool> Create2DMediaInstance(byte[] bytes, string path, string name, bool isVideo, Node3D place, Rect2 slot)
    {
        var item = Media2DInstance.Create(_outputTemplate, _outputRoot, GeneratedMediaGroup, CellPadding, place, slot);

        if (isVideo)
            await Video2DOutput.Set(item, bytes, path, name);
        else
            Image2DOutput.Set(item, bytes, path);

        Logger.Info($"Placed {(isVideo ? "video" : "image")} '{name}'.");
        _mediaInstances.Add(item);
        return true;
    }
}