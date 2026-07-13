using System.Collections.Generic;
using BCSVRMuseum.Museum_Scripts.Decision;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Object3D;

public sealed class Object3DDisplayInstance
{
	private Object3DDisplayInstance(Node3D item)
	{
		Item = item;
	}

	public Node3D Item { get; }

	public static Object3DDisplayInstance Create(Node3D template, Node3D displayRoot, string groupName)
	{
		var item = (Node3D)template.Duplicate();
		item.AddToGroup(groupName);
		displayRoot.AddChild(item);
		NodeTreeActivator.SetActive(item, true);

		return new Object3DDisplayInstance(item);
	}

	public void StoreRetrievableMetadata(IReadOnlyList<double> vector, string mediaName, string mediaPath)
	{
		RetrievableMetadata.Store(Item, vector, mediaName, mediaPath);
		ConfigureActionPopups(vector, mediaPath);
	}

	private void ConfigureActionPopups(IReadOnlyList<double> vector, string mediaPath)
	{
		foreach (var child in Item.FindChildren("*", "", true, false))
		{
			if (child is not DisplayActionPopup popup)
				continue;
			NodeTreeActivator.SetActive(popup.GetParent<Node3D>(), false);
			popup.SetVector(vector);
			popup.SetSourcePath(mediaPath);
		}
	}
}
