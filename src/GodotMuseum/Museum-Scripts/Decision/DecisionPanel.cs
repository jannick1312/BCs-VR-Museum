using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Decision;

public partial class DecisionPanel : Node
{
	[Signal]
	public delegate void DismissRequestedEventHandler();

	private readonly List<double> _vector = [];
	private static readonly EventLogger Log = new(nameof(DecisionPanel));

	public override void _Ready()
	{
		GetNode<Button>("../Panel/OriginalSize").Pressed += () => OnDecisionButtonPressed("Original Size");
		GetNode<Button>("../Panel/SimilaritySearch").Pressed += () => OnDecisionButtonPressed("Similarity Search");
	}

	public void SetVector(string vectorJson)
	{
		_vector.Clear();

		var vector = JsonSerializer.Deserialize<List<double>>(vectorJson)!;
		_vector.AddRange(vector);
	}

	private void OnDecisionButtonPressed(string action)
	{
		Log.Info($"{action}: first={_vector[0]}, last={_vector[^1]}.");
		EmitSignal(SignalName.DismissRequested);
	}
}