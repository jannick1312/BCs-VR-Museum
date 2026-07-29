using System;
using System.Collections.Generic;
using BCSVRMuseum.Museum_Scripts.Decision;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using Godot;
using Models;

namespace BCSVRMuseum.Museum_Scripts.Placement.Object3D;

public sealed class Object3DDisplayInstance
{
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

	public static Object3DDisplayInstance Create(Node3D template, Node3D displayRoot, string groupName)
	{
		var item = (Node3D)template.Duplicate();
		item.AddToGroup(groupName);
		displayRoot.AddChild(item);
		NodeTreeActivator.SetActive(item, true);

		return new Object3DDisplayInstance(item);
	}

	public void AttachObject(Node3D objectNode, Aabb originalBounds)
	{
		ObjectNode = objectNode;
		OriginalBounds = originalBounds;
		OriginalObjectTransform = objectNode.Transform;
	}

	public void StoreDisplayPlacement()
	{
		DisplayParent = ObjectNode.GetParent<Node3D>();
		DisplayTransform = ObjectNode.Transform;
	}

	public void RestoreToDisplay()
	{
		ObjectNode.Reparent(DisplayParent, false);
		ObjectNode.Transform = DisplayTransform;
	}

	public void StoreRetrievableMetadata(IReadOnlyList<double> vector, string mediaName, string mediaPath, MediaMetadata metadata, Action showOriginalSize)
	{
		RetrievableMetadata.Store(Item, vector, mediaName, mediaPath, metadata);
		ConfigureActionPopups(vector, mediaPath, showOriginalSize);
	}

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
