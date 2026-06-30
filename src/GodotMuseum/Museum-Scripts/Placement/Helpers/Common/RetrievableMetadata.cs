using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;

public static class RetrievableMetadata
{
    private const string ClipVectorKey = "clip_vector";
    private const string MediaNameKey = "media_name";

    public static void Store(Node node, IReadOnlyList<double> vector, string mediaName)
    {
        StoreRecursive(node, JsonSerializer.Serialize(vector), mediaName);
    }

    private static void StoreRecursive(Node node, string vectorJson, string mediaName)
    {
        node.SetMeta(ClipVectorKey, vectorJson);
        node.SetMeta(MediaNameKey, mediaName);

        foreach (var child in node.GetChildren())
            StoreRecursive(child, vectorJson, mediaName);
    }
}