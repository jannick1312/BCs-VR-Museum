using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;

/// <summary>
/// Represents a placement area and the number of media items that fit there.
/// </summary>
/// <param name="place">The node defining the placement area.</param>
/// <param name="maxItems">The maximum number of items assigned to the area.</param>
public sealed class PlacementGroup(Node3D place, int maxItems)
{
	public Node3D Place { get; } = place;
	public int MaxItems { get; } = Mathf.Max(1, maxItems);
}
