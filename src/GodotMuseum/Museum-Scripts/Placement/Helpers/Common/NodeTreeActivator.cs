using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;

/// <summary>
/// Turns the visible and clickable parts of a node tree on or off.
/// </summary>
public static class NodeTreeActivator
{
	/// <summary>
	/// Updates visibility, collision, and input for a node and all its children.
	/// </summary>
	/// <param name="node">The root node to update.</param>
	/// <param name="active">If the node tree should be active.</param>
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
