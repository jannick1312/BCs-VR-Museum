using System.Collections.Generic;
using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Object3D;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using Godot;
using Logger;
using Models;

namespace BCSVRMuseum.Museum_Scripts.Placement.Object3D;

public sealed class Object3DPlacementStrategy : PlacementStrategyBase
{
    private const string GeneratedObjectGroup = "Generated3DObject";

    private readonly Object3DDisplayFitter _fitter;
    private static readonly EventLogger Log = new(nameof(Object3DPlacementStrategy));

    public Object3DPlacementStrategy(Node owner, Node3D displayRoot, Node placesRoot) : base(owner, displayRoot, placesRoot, GeneratedObjectGroup, "3D object", Log)
    {
        if (IsReady)
            _fitter = new Object3DDisplayFitter(DisplayTemplate);
    }

    public async Task Place(IReadOnlyList<DisplayMediaItem> objectItems)
    {
        ClearGenerated();

        if (!IsReady || _fitter == null)
            return;

        var placeGroups = PlaceCollector.Collect(PlacesRoot, 1);
        var count = Mathf.Min(objectItems.Count, placeGroups.Count);
        var placedObjectCount = 0;

        for (var i = 0; i < count; i++)
        {
            await WaitForFrame();

            var objectItem = objectItems[i];
            var instance = Object3DDisplayInstance.Create(DisplayTemplate, DisplayRoot, GeneratedGroup);
            var objectScale = Object3DMediaRenderer.Render(instance, objectItem.Bytes, objectItem.Path, placeGroups[i].Place, _fitter);

            if (objectScale == null)
            {
                instance.Item.QueueFree();
                Log.Warning($"Skipping 3D object '{objectItem.Name}' at index {i} because it could not be loaded.");
                continue;
            }

            Log.Info($"Placed 3D object '{objectItem.Name}'. ObjectScale={objectScale}");
            placedObjectCount++;
        }

        if (placedObjectCount < objectItems.Count)
            Log.Warning($"Placed {placedObjectCount} of {objectItems.Count} 3D objects.");
        else
            Log.Info($"Placed all {placedObjectCount} 3D objects.");
    }
}