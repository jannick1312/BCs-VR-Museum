using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Models;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;

public static class RetrievableMetadata
{
	private const string ClipVectorKey = "clip_vector";
	private const string MediaNameKey = "media_name";
	private const string MediaPathKey = "media_path";
	private const string MetadataKey = "retrievable_metadata";

	public static void Store(Node node, IReadOnlyList<double> vector, string mediaName, string mediaPath, MediaMetadata metadata)
	{
		StoreRecursive(node, JsonSerializer.Serialize(vector), mediaName, mediaPath, JsonSerializer.Serialize(metadata));
	}

	private static void StoreRecursive(Node node, string vectorJson, string mediaName, string mediaPath, string metadata)
	{
		node.SetMeta(ClipVectorKey, vectorJson);
		node.SetMeta(MediaNameKey, mediaName);
		node.SetMeta(MediaPathKey, mediaPath);
		node.SetMeta(MetadataKey, metadata);

		foreach (var child in node.GetChildren())
			StoreRecursive(child, vectorJson, mediaName, mediaPath, metadata);
	}
}
