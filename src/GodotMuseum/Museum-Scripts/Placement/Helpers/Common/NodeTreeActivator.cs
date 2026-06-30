using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;

public static class NodeTreeActivator
{
	public static void SetActive(Node node, bool active)
	{
		if (node is Node3D node3D)
			node3D.Visible = active;

		if (node is CollisionShape3D collisionShape)
			collisionShape.SetDeferred(CollisionShape3D.PropertyName.Disabled, !active);

		if (node is Viewport viewport)
			viewport.SetProcessInput(active);

		foreach (var child in node.GetChildren())
			SetActive(child, active);
	}
}