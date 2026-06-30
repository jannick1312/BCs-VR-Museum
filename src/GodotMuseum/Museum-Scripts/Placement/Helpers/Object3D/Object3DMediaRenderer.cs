using System.IO;
using BCSVRMuseum.Museum_Scripts.Placement.Object3D;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Object3D;

public static class Object3DMediaRenderer
{
    public static float? Render(Object3DDisplayInstance instance, byte[] bytes, string path, Node3D place, Object3DDisplayFitter fitter)
    {
        var objectNode = Load(bytes, path);
        return objectNode == null ? null : fitter.Place(instance.Item, objectNode, place);
    }

    private static Node3D Load(byte[] bytes, string path)
    {
        var gltf = new GltfDocument();
        var state = new GltfState();

        if (File.Exists(path))
            gltf.AppendFromFile(path, state);
        else
            gltf.AppendFromBuffer(bytes, "", state);

        return gltf.GenerateScene(state) as Node3D;
    }
}