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

	public static bool TryRead(Node node, out IReadOnlyList<double> vector, out string mediaName)
	{
		vector = [];
		mediaName = string.Empty;

		for (var current = node; current != null; current = current.GetParent())
		{
			if (!current.HasMeta(ClipVectorKey))
				continue;

			var vectorJson = current.GetMeta(ClipVectorKey).AsString();
			var parsed = JsonSerializer.Deserialize<List<double>>(vectorJson)!;

			vector = parsed;
			mediaName = current.GetMeta(MediaNameKey).AsString();
			return true;
		}
		return false;
	}

	public static string SerializeVector(IReadOnlyList<double> vector)
	{
		return JsonSerializer.Serialize(vector);
	}

	private static void StoreRecursive(Node node, string vectorJson, string mediaName)
	{
		node.SetMeta(ClipVectorKey, vectorJson);
		node.SetMeta(MediaNameKey, mediaName);

		foreach (var child in node.GetChildren())
			StoreRecursive(child, vectorJson, mediaName);
	}
}