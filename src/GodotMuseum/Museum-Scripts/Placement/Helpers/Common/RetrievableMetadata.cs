using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Models;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;

/// <summary>
/// Stores media data on display nodes.
/// </summary>
public static class RetrievableMetadata
{
	private const string ClipVectorKey = "clip_vector";
	private const string MediaNameKey = "media_name";
	private const string MediaPathKey = "media_path";
	private const string MetadataKey = "retrievable_metadata";

	/// <summary>
	/// Stores media data on a node and all its children.
	/// </summary>
	/// <param name="node">The root node that receives the data.</param>
	/// <param name="vector">The feature vector for the media.</param>
	/// <param name="mediaName">The display name of the media.</param>
	/// <param name="mediaPath">The media file path.</param>
	/// <param name="metadata">The metadata for the media.</param>
	public static void Store(Node node, IReadOnlyList<double> vector, string mediaName, string mediaPath, MediaMetadata metadata)
	{
		StoreRecursive(node, JsonSerializer.Serialize(vector), mediaName, mediaPath, JsonSerializer.Serialize(metadata));
	}

	/// <summary>
	/// Stores media data on a node and all its children.
	/// </summary>
	/// <param name="node">The current node that receives the data.</param>
	/// <param name="vectorJson">The stored feature vector.</param>
	/// <param name="mediaName">The display name of the media.</param>
	/// <param name="mediaPath">The media file path.</param>
	/// <param name="metadata">The stored metadata.</param>
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
