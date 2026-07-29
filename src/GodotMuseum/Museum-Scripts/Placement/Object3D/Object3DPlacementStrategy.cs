using System.Collections.Generic;
using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Object3D;
using Godot;
using Logger;
using Models;

namespace BCSVRMuseum.Museum_Scripts.Placement.Object3D;

public sealed class Object3DPlacementStrategy : PlacementStrategyBase
{
	private const string GeneratedObjectGroup = "Generated3DObject";
	private static readonly EventLogger Log = new(nameof(Object3DPlacementStrategy));
	private readonly Object3DDisplayFitter _fitter;
	private readonly OriginalSizeController _originalSizeController;

	public Object3DPlacementStrategy(Node owner, Node3D displayRoot, Node placesRoot, OriginalSizeController originalSizeController) : base(owner, displayRoot, placesRoot, GeneratedObjectGroup)
	{
		_fitter = new Object3DDisplayFitter(DisplayTemplate);
		_originalSizeController = originalSizeController;
	}

	public int GetCapacity()
	{
		return PlaceCollector.Collect(PlacesRoot, 1).Count;
	}

	public async Task Place(IReadOnlyList<DisplayMediaItem> objectItems)
	{
		_originalSizeController?.Reset();
		ClearGenerated();

		var placeGroups = PlaceCollector.Collect(PlacesRoot, 1);
		var count = Mathf.Min(objectItems.Count, placeGroups.Count);
		var placementTasks = new List<Task<bool>>();

		for (var i = 0; i < count; i++)
		{
			await WaitForFrame();

			placementTasks.Add(PlaceObject(objectItems[i], placeGroups[i].Place, i));
		}

		var placementResults = await Task.WhenAll(placementTasks);
		var placedObjectCount = 0;
		foreach (var placed in placementResults)
			if (placed)
				placedObjectCount++;

		if (placedObjectCount < objectItems.Count)
			Log.Warning($"Placed {placedObjectCount} of {objectItems.Count} 3D objects.");
		else
			Log.Info($"Placed all {placedObjectCount} 3D objects.");
	}

	private async Task<bool> PlaceObject(DisplayMediaItem objectItem, Node3D place, int index)
	{
		var instance = Object3DDisplayInstance.Create(DisplayTemplate, DisplayRoot, GeneratedGroup);
		instance.StoreRetrievableMetadata(objectItem.Vector, objectItem.Name, objectItem.Path, objectItem.Metadata, () => _originalSizeController.ShowOriginalSize(instance));
		var objectScale = await Object3DMediaRenderer.Render(instance, objectItem.Path, place, _fitter);

		if (objectScale == null)
		{
			instance.Item.QueueFree();
			Log.Warning($"Skipping 3D object '{objectItem.Name}' at index {index} because it could not be loaded.");
			return false;
		}

		Log.Info($"Placed 3D object '{objectItem.Name}'. ObjectScale={objectScale}");
		return true;
	}
}
