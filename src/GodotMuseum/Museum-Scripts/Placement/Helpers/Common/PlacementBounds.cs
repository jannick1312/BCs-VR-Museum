using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;

public static class PlacementBounds
{
	public static Vector2 MeshAreaSize(Node3D place)
	{
		var mesh = (MeshInstance3D)place;
		var size = mesh.GetAabb().Size;
		var scale = mesh.Scale.Abs();

		return new Vector2(Mathf.Max(0.1f, size.X * scale.X), Mathf.Max(0.1f, size.Y * scale.Y));
	}

	public static Aabb ScaledMeshBounds(MeshInstance3D mesh)
	{
		var bounds = mesh.GetAabb();
		bounds.Size *= mesh.Scale.Abs();
		return bounds;
	}
}
