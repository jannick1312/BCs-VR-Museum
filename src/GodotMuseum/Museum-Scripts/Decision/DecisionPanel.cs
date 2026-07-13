using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Decision;

public partial class DecisionPanel : Node
{
	[Signal]
	public delegate void DismissRequestedEventHandler();
	[Signal]
	public delegate void SimilaritySearchRequestedEventHandler(string vectorJson);

	private static readonly EventLogger Log = new(nameof(DecisionPanel));
	private string _vectorJson = string.Empty;

	public override void _Ready()
	{
		GetNode<Button>("../Panel/OriginalSize").Pressed += () => OnDecisionButtonPressed("Original Size");
		GetNode<Button>("../Panel/SimilaritySearch").Pressed += OnSimilaritySearchPressed;
	}

	public void SetVector(string vectorJson)
	{
		_vectorJson = vectorJson;
	}

	private void OnDecisionButtonPressed(string action)
	{
		Log.Info($"Display action selected. Action='{action}'.");
		EmitSignal(SignalName.DismissRequested);
	}

	private void OnSimilaritySearchPressed()
	{
		EmitSignal(SignalName.SimilaritySearchRequested, _vectorJson);
		EmitSignal(SignalName.DismissRequested);
	}
}
