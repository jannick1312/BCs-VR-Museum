using Godot;
using Infrastructure.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BCSVRMuseum.Museum_Scripts;

public partial class ObjectOutputSetter : Node
{
    [Export] public NodePath OutputPlacesPath;

    private Node _outputPlacesRoot;
    private static readonly EventLogger Logger = new(nameof(ObjectOutputSetter));

    public override void _Ready()
    {
        _outputPlacesRoot = GetNodeOrNull(OutputPlacesPath);

        if (_outputPlacesRoot == null)
        {
            Logger.Error("3D object output places root is missing.");
            return;
        }

        ClearGeneratedObjects();
    }

    public async Task SetOutputObjectsFromBytes(IReadOnlyList<byte[]> objectBytes)
    {
        ClearGeneratedObjects();

        var places = GetOutputPlaces();

        var count = Mathf.Min(objectBytes.Count, places.Count);

        if (objectBytes.Count > places.Count)
            Logger.Warning($"Only {count} of {objectBytes.Count} 3D objects can be placed.");

        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            var objectNode = LoadObject(objectBytes[i]);

            if (objectNode == null)
            {
                Logger.Warning($"Skipping 3D object at index {i} because it could not be loaded.");
                continue;
            }

            PlaceObject(objectNode, places[i]);
        }
    }

    private void ClearGeneratedObjects()
    {
        foreach (var child in GetChildren())
        {
            if (child != null && child.IsInGroup("GeneratedOutputObject"))
                child.QueueFree();
        }
    }

    private static Node3D LoadObject(byte[] bytes)
    {
        var gltf = new GltfDocument();
        var state = new GltfState();

        var error = gltf.AppendFromBuffer(bytes, "", state);

        if (error == Error.Ok) return gltf.GenerateScene(state) as Node3D;
        Logger.Error($"Could not load 3D object. Error: {error}");
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

    private static Aabb GetPlaceBounds(Node3D place)
    {
        var mesh = (MeshInstance3D)place;
        var aabb = mesh.GetAabb();
        aabb.Size *= mesh.Scale.Abs();  
        return aabb;
    }

    private void PlaceObject(Node3D item, Node3D place)
    {
        item.AddToGroup("GeneratedOutputObject");
        AddChild(item);

        item.GlobalTransform = place.GlobalTransform;

        var objectBounds = GetObjectBounds(item);
        var placeBounds = GetPlaceBounds(place);

        var scale = GetScale(objectBounds.Size, placeBounds.Size);

        Logger.Info($"Placing 3D object. Scale={scale}");

        item.Scale *= scale;

        objectBounds = GetObjectBounds(item);
        var centerOffset = objectBounds.GetCenter();
        item.Position -= centerOffset * item.Scale;
    }

    private static float GetScale(Vector3 objectSize, Vector3 targetSize)
    {
        if (objectSize.X <= 0 || objectSize.Y <= 0 || objectSize.Z <= 0)
        {
            Logger.Warning($"Invalid 3D object size. Using scale 1.");
            return 1.0f;
        }

        var sx = targetSize.X / objectSize.X;
        var sy = targetSize.Y / objectSize.Y;
        var sz = targetSize.Z / objectSize.Z;

        return Mathf.Min(sx, Mathf.Min(sy, sz));
    }

    private static Aabb GetObjectBounds(Node3D root)
    {
        var hasBounds = false;
        var bounds = new Aabb();

        foreach (var node in root.FindChildren("*", "MeshInstance3D", true, false))
        {
            if (node is not MeshInstance3D mesh)
                continue;

            var meshAabb = TransformAabb(root.GlobalTransform.AffineInverse() * mesh.GlobalTransform, mesh.GetAabb());

            bounds = hasBounds ? bounds.Merge(meshAabb) : meshAabb;
            hasBounds = true;
        }

        if (!hasBounds)
            Logger.Warning($"3D object '{root.Name}' has no MeshInstance3D children. Using fallback bounds.");

        return hasBounds ? bounds : new Aabb(Vector3.Zero, Vector3.One);
    }

    private static Aabb TransformAabb(Transform3D transform, Aabb aabb)
    {
        var min = aabb.Position;
        var max = aabb.End;

        var points = new[]
        {
            new Vector3(min.X, min.Y, min.Z),
            new Vector3(max.X, min.Y, min.Z),
            new Vector3(min.X, max.Y, min.Z),
            new Vector3(max.X, max.Y, min.Z),
            new Vector3(min.X, min.Y, max.Z),
            new Vector3(max.X, min.Y, max.Z),
            new Vector3(min.X, max.Y, max.Z),
            new Vector3(max.X, max.Y, max.Z),
        };

        var result = new Aabb(transform * points[0], Vector3.Zero);

        for (var i = 1; i < points.Length; i++)
            result = result.Expand(transform * points[i]);

        return result;
    }
}