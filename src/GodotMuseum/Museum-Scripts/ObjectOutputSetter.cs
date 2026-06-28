using Godot;
using Infrastructure.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BCSVRMuseum.Museum_Scripts;

public partial class ObjectOutputSetter : Node
{
    [Export] public NodePath OutputInstancePath;
    [Export] public NodePath OutputPlacesPath;

    private Node3D _outputRoot;
    private Node3D _outputTemplate;
    private Node _outputPlacesRoot;
    private ObjectOutputFitter _fitter;
    private static readonly EventLogger Logger = new(nameof(ObjectOutputSetter));

    public override void _Ready()
    {
        _outputRoot = GetNodeOrNull<Node3D>(OutputInstancePath);
        _outputPlacesRoot = GetNodeOrNull(OutputPlacesPath);

        if (_outputRoot == null)
        {
            Logger.Error("3D object output root is missing.");
            return;
        }

        if (_outputPlacesRoot == null)
        {
            Logger.Error("3D object output places root is missing.");
            return;
        }

        ClearGeneratedObjects();
        _outputTemplate = _outputRoot.Duplicate() as Node3D;

        if (_outputTemplate == null)
        {
            Logger.Error("3D object output template could not be duplicated as Node3D.");
            return;
        }

        _fitter = new ObjectOutputFitter(_outputTemplate);
    }

    public async Task SetOutputObjects(IReadOnlyList<byte[]> objectBytes, IReadOnlyList<string> objectPaths, IReadOnlyList<string> objectNames)
    {
        ClearGeneratedObjects();

        var places = GetOutputPlaces();
        
        var count = Mathf.Min(objectBytes.Count, places.Count);
        var placedObjectCount = 0;

        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            var objectNode = LoadObject(objectBytes[i], objectPaths[i]);

            if (objectNode == null)
            {
                Logger.Warning($"Skipping 3D object '{objectNames[i]}' at index {i} because it could not be loaded.");
                continue;
            }

            PlaceObject(objectNode, objectNames[i], places[i]);
            placedObjectCount++;
        }

        if (placedObjectCount < objectBytes.Count)
            Logger.Warning($"Placed {placedObjectCount} of {objectBytes.Count} 3D objects.");
        else
            Logger.Info($"Placed all {placedObjectCount} 3D objects.");
    }

    private void ClearGeneratedObjects()
    {
        foreach (var child in _outputRoot.GetChildren())
        {
            if (child != null && child.IsInGroup("GeneratedOutputObject"))
                child.QueueFree();
        }
    }

    private static Node3D LoadObject(byte[] bytes, string path)
    {
        var gltf = new GltfDocument();
        var state = new GltfState();

        if (System.IO.File.Exists(path))
            gltf.AppendFromFile(path, state);
        else
            gltf.AppendFromBuffer(bytes, "", state);

        return gltf.GenerateScene(state) as Node3D;
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

    private void PlaceObject(Node3D objectNode, string objectName, Node3D place)
    {
        var item = (Node3D)_outputTemplate.Duplicate();
        item.AddToGroup("GeneratedOutputObject");

        _outputRoot.AddChild(item);
        SetTreeActive(item, true);

        var objectScale = _fitter.Place(item, objectNode, place);
        Logger.Info($"Placed 3D object '{objectName}'. ObjectScale={objectScale}");
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