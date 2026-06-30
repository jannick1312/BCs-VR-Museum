using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;

public sealed class PlacementGroup(Node3D place, int maxItems)
{
    public Node3D Place { get; } = place;
    public int MaxItems { get; } = Mathf.Max(1, maxItems);
}