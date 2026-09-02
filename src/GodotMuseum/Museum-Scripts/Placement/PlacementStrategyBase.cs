using System.Threading.Tasks;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement;

/// <summary>
/// Provides shared setup and cleanup for media placement.
/// </summary>
public abstract class PlacementStrategyBase
{
	/// <summary>
	/// Prepares placement and copies the display template.
	/// </summary>
	/// <param name="owner">The node that runs placement tasks.</param>
	/// <param name="displayRoot">The template root used to create displays.</param>
	/// <param name="placesRoot">The root containing placement areas.</param>
	/// <param name="generatedGroup">The group used for created displays.</param>
	protected PlacementStrategyBase(Node owner, Node3D displayRoot, Node placesRoot, string generatedGroup)
	{
		Owner = owner;
		DisplayRoot = displayRoot;
		PlacesRoot = placesRoot;
		GeneratedGroup = generatedGroup;
		ClearGenerated();
		DisplayTemplate = (Node3D)DisplayRoot.Duplicate();
	}

	private Node Owner { get; }
	protected Node3D DisplayRoot { get; }
	protected Node3D DisplayTemplate { get; private set; }
	protected Node PlacesRoot { get; }
	protected string GeneratedGroup { get; }

	/// <summary>
	/// Removes display nodes created by the previous placement.
	/// </summary>
	protected void ClearGenerated()
	{
		foreach (var child in DisplayRoot.GetChildren())
			if (child.IsInGroup(GeneratedGroup))
				child.QueueFree();
	}

	/// <summary>
	/// Waits for the next process frame.
	/// </summary>
	/// <returns>A task that completes on the next process frame.</returns>
	protected async Task WaitForFrame()
	{
		await Owner.ToSignal(Owner.GetTree(), SceneTree.SignalName.ProcessFrame);
	}
}
