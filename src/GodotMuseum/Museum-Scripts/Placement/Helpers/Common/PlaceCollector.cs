using System.Collections.Generic;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;

public static class PlaceCollector
{
	public static List<PlacementGroup> Collect(Node placesRoot, int defaultMaxItems)
	{
		var result = new List<PlacementGroup>();

		foreach (var child in placesRoot.GetChildren())
			CollectChild(child, Mathf.Max(1, defaultMaxItems), result);

		return result;
	}

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