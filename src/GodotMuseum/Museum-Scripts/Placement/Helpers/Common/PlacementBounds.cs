using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;

/// <summary>
/// Measures meshes used for media placement.
/// </summary>
public static class PlacementBounds
{
	/// <summary>
	/// Gets the scaled width and height of a mesh placement area.
	/// </summary>
	/// <param name="place">The mesh placement area.</param>
	/// <returns>The placement area's scaled size.</returns>
	public static Vector2 MeshAreaSize(Node3D place)
	{
		var mesh = (MeshInstance3D)place;
		var size = mesh.GetAabb().Size;
		var scale = mesh.Scale.Abs();

		return new Vector2(Mathf.Max(0.1f, size.X * scale.X), Mathf.Max(0.1f, size.Y * scale.Y));
	}

	/// <summary>
	/// Gets the mesh box with its scale applied to the size.
	/// </summary>
	/// <param name="mesh">The mesh to measure.</param>
	/// <returns>The scaled mesh bounds.</returns>
	public static Aabb ScaledMeshBounds(MeshInstance3D mesh)
	{
		var bounds = mesh.GetAabb();
		bounds.Size *= mesh.Scale.Abs();
		return bounds;
	}
}
