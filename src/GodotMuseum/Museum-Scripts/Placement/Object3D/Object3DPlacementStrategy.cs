using System.Collections.Generic;
using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Object3D;
using Godot;
using Logger;
using Models;

namespace BCSVRMuseum.Museum_Scripts.Placement.Object3D;

/// <summary>
/// Loads and places 3D models in museum displays.
/// </summary>
public sealed class Object3DPlacementStrategy : PlacementStrategyBase
{
	private const string GeneratedObjectGroup = "Generated3DObject";
	private static readonly EventLogger Log = new(nameof(Object3DPlacementStrategy));
	private readonly Object3DDisplayFitter _fitter;
	private readonly OriginalSizeController _originalSizeController;

	/// <summary>
	/// Sets up 3D model placement for the display areas.
	/// </summary>
	/// <param name="owner">The node that runs placement tasks.</param>
	/// <param name="displayRoot">The template root used to create displays.</param>
	/// <param name="placesRoot">The root containing 3D model placement areas.</param>
	/// <param name="originalSizeController">The controller used for original-size viewing.</param>
	public Object3DPlacementStrategy(Node owner, Node3D displayRoot, Node placesRoot, OriginalSizeController originalSizeController) : base(owner, displayRoot, placesRoot, GeneratedObjectGroup)
	{
		_fitter = new Object3DDisplayFitter(DisplayTemplate);
		_originalSizeController = originalSizeController;
	}

	/// <summary>
	/// Gets the number of places for 3D models.
	/// </summary>
	/// <returns>The total number of places for 3D models.</returns>
	public int GetCapacity()
	{
		return PlaceCollector.Collect(PlacesRoot, 1).Count;
	}

	/// <summary>
	/// Places 3D models on the museum displays.
	/// </summary>
	/// <param name="objectItems">The 3D models to place.</param>
	/// <returns>A task that completes when 3D model placement finishes.</returns>
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

	/// <summary>
	/// Creates one display and loads its 3D model.
	/// </summary>
	/// <param name="objectItem">The media item describing the 3D model.</param>
	/// <param name="place">The museum placement area.</param>
	/// <param name="index">The 3D model's index in the current placement.</param>
	/// <returns>A task containing <see langword="true"/> if the 3D model was loaded and placed and <see langword="false"/> otherwise.</returns>
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
