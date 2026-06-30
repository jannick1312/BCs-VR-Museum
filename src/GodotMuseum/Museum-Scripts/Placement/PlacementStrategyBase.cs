using System.Threading.Tasks;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement;

public abstract class PlacementStrategyBase
{
	private Node Owner { get; }
	protected Node3D DisplayRoot { get; }
	protected Node3D DisplayTemplate { get; private set; }
	protected Node PlacesRoot { get; }
	protected string GeneratedGroup { get; }

	protected PlacementStrategyBase(Node owner, Node3D displayRoot, Node placesRoot, string generatedGroup)
	{
		Owner = owner;
		DisplayRoot = displayRoot;
		PlacesRoot = placesRoot;
		GeneratedGroup = generatedGroup;

		ClearGenerated();
		DisplayTemplate = (Node3D)DisplayRoot.Duplicate();
	}

	protected void ClearGenerated()
	{
		foreach (var child in DisplayRoot.GetChildren())
		{
			if (child.IsInGroup(GeneratedGroup))
				child.QueueFree();
		}
	}

	protected async Task WaitForFrame()
	{
		await Owner.ToSignal(Owner.GetTree(), SceneTree.SignalName.ProcessFrame);
	}
}