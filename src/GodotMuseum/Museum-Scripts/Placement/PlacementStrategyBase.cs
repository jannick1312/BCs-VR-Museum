using System.Threading.Tasks;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Placement;

public abstract class PlacementStrategyBase
{
    private Node Owner { get; }
    protected Node3D DisplayRoot { get; }
    protected Node3D DisplayTemplate { get; private set; }
    protected Node PlacesRoot { get; }
    protected string GeneratedGroup { get; }
    private EventLogger Logger { get; }

    protected PlacementStrategyBase(Node owner, Node3D displayRoot, Node placesRoot, string generatedGroup, string displayName, EventLogger logger)
    {
        Owner = owner;
        DisplayRoot = displayRoot;
        PlacesRoot = placesRoot;
        GeneratedGroup = generatedGroup;
        Logger = logger;

        if (DisplayRoot == null)
        {
            Logger.Error($"{displayName} display instance root is missing.");
            return;
        }

        if (PlacesRoot == null)
        {
            Logger.Error($"{displayName} places root is missing.");
            return;
        }

        ClearGenerated();
        DisplayTemplate = DisplayRoot.Duplicate() as Node3D;

        if (DisplayTemplate == null)
            Logger.Error($"{displayName} display template could not be duplicated as Node3D.");
    }

    protected bool IsReady => DisplayRoot != null && DisplayTemplate != null && PlacesRoot != null;

    protected void ClearGenerated()
    {
        if (DisplayRoot == null)
            return;

        foreach (var child in DisplayRoot.GetChildren())
        {
            if (child != null && child.IsInGroup(GeneratedGroup))
                child.QueueFree();
        }
    }

    protected async Task WaitForFrame()
    {
        await Owner.ToSignal(Owner.GetTree(), SceneTree.SignalName.ProcessFrame);
    }
}