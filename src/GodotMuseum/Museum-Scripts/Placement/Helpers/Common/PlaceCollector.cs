using System.Collections.Generic;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;

/// <summary>
/// Finds mesh placement areas and their item limits.
/// </summary>
public static class PlaceCollector
{
	/// <summary>
	/// Finds all mesh placement areas below a root node.
	/// </summary>
	/// <param name="placesRoot">The root containing placement areas.</param>
	/// <param name="defaultMaxItems">The item limit used when a group has no numeric name.</param>
	/// <returns>The found placement groups.</returns>
	public static List<PlacementGroup> Collect(Node placesRoot, int defaultMaxItems)
	{
		var result = new List<PlacementGroup>();

		foreach (var child in placesRoot.GetChildren())
			CollectChild(child, Mathf.Max(1, defaultMaxItems), result);

		return result;
	}

	/// <summary>
	/// Searches a node branch and uses the item limit from its parent.
	/// </summary>
	/// <param name="child">The node to inspect.</param>
	/// <param name="inheritedMaxItems">The item limit from the parent group.</param>
	/// <param name="result">The list that receives found placement groups.</param>
	private static void CollectChild(Node child, int inheritedMaxItems, List<PlacementGroup> result)
	{
		if (child is MeshInstance3D meshPlace)
		{
			result.Add(new PlacementGroup(meshPlace, inheritedMaxItems));
			return;
		}

		var maxItems = int.TryParse(child.Name, out var parsedMaxItems) && parsedMaxItems > 0 ? parsedMaxItems : inheritedMaxItems;

		foreach (var grandChild in child.GetChildren())
			CollectChild(grandChild, maxItems, result);
	}
}
