using System;
using System.Collections.Generic;
using BCSVRMuseum.Museum_Scripts.Decision;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using Godot;
using Models;

namespace BCSVRMuseum.Museum_Scripts.Placement.Object3D;

/// <summary>
/// Creates and manages one 3D model display.
/// </summary>
public sealed class Object3DDisplayInstance
{
	/// <summary>
	/// Sets up a created 3D model display.
	/// </summary>
	/// <param name="item">The root node of the created display.</param>
	private Object3DDisplayInstance(Node3D item)
	{
		Item = item;
	}

	public Node3D Item { get; }
	public Node3D ObjectNode { get; private set; }
	public Aabb OriginalBounds { get; private set; }
	public Transform3D OriginalObjectTransform { get; private set; }
	private Node3D DisplayParent { get; set; }
	private Transform3D DisplayTransform { get; set; }

	/// <summary>
	/// Creates and shows a display from the template.
	/// </summary>
	/// <param name="template">The display template to duplicate.</param>
	/// <param name="displayRoot">The node that receives the created display.</param>
	/// <param name="groupName">The group used for created displays.</param>
	/// <returns>The created display.</returns>
	public static Object3DDisplayInstance Create(Node3D template, Node3D displayRoot, string groupName)
	{
		var item = (Node3D)template.Duplicate();
		item.AddToGroup(groupName);
		displayRoot.AddChild(item);
		NodeTreeActivator.SetActive(item, true);

		return new Object3DDisplayInstance(item);
	}

	/// <summary>
	/// Stores a loaded 3D model and its original bounds.
	/// </summary>
	/// <param name="objectNode">The loaded 3D model.</param>
	/// <param name="originalBounds">The model's bounds before display fitting.</param>
	public void AttachObject(Node3D objectNode, Aabb originalBounds)
	{
		ObjectNode = objectNode;
		OriginalBounds = originalBounds;
		OriginalObjectTransform = objectNode.Transform;
	}

	/// <summary>
	/// Saves the 3D model's parent, position, rotation, and scale.
	/// </summary>
	public void StoreDisplayPlacement()
	{
		DisplayParent = ObjectNode.GetParent<Node3D>();
		DisplayTransform = ObjectNode.Transform;
	}

	/// <summary>
	/// Returns the 3D model from an original-size room to its museum display.
	/// </summary>
	public void RestoreToDisplay()
	{
		ObjectNode.Reparent(DisplayParent, false);
		ObjectNode.Transform = DisplayTransform;
	}

	/// <summary>
	/// Adds search data and sets up the popups.
	/// </summary>
	/// <param name="vector">The feature vector for the 3D model.</param>
	/// <param name="mediaName">The display name of the 3D model.</param>
	/// <param name="mediaPath">The media file path.</param>
	/// <param name="metadata">The metadata for the 3D model.</param>
	/// <param name="showOriginalSize">The action that opens the original-size room.</param>
	public void StoreRetrievableMetadata(IReadOnlyList<double> vector, string mediaName, string mediaPath, MediaMetadata metadata, Action showOriginalSize)
	{
		RetrievableMetadata.Store(Item, vector, mediaName, mediaPath, metadata);
		ConfigureActionPopups(vector, mediaPath, showOriginalSize);
	}

	/// <summary>
	/// Sets search and original-size actions.
	/// </summary>
	/// <param name="vector">The feature vector for the 3D model.</param>
	/// <param name="mediaPath">The media file path.</param>
	/// <param name="showOriginalSize">The action that opens the original-size room.</param>
	private void ConfigureActionPopups(IReadOnlyList<double> vector, string mediaPath, Action showOriginalSize)
	{
		foreach (var child in Item.FindChildren("*", "", true, false))
		{
			if (child is not DisplayActionPopup popup)
				continue;
			NodeTreeActivator.SetActive(popup.GetParent<Node3D>(), false);
			popup.SetOriginalSizeHandler(showOriginalSize);
			popup.SetVector(vector);
			popup.SetSourcePath(mediaPath);
		}
	}
}
