using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Decision;

public partial class DecisionPanel : Node
{
	[Signal]
	public delegate void DismissRequestedEventHandler();

	[Signal]
	public delegate void SimilaritySearchRequestedEventHandler(string vectorJson);

	private readonly List<double> _vector = [];
	private static readonly EventLogger Log = new(nameof(DecisionPanel));

	public override void _Ready()
	{
		GetNode<Button>("../Panel/OriginalSize").Pressed += () => OnDecisionButtonPressed("Original Size");
		GetNode<Button>("../Panel/SimilaritySearch").Pressed += OnSimilaritySearchPressed;
	}

	public void SetVector(string vectorJson)
	{
		_vector.Clear();

		var vector = JsonSerializer.Deserialize<List<double>>(vectorJson)!;
		_vector.AddRange(vector);
	}

	private void OnDecisionButtonPressed(string action)
	{
		Log.Info(action);
		EmitSignal(SignalName.DismissRequested);
	}

	private void OnSimilaritySearchPressed()
	{
		EmitSignal(SignalName.SimilaritySearchRequested, JsonSerializer.Serialize(_vector));
		EmitSignal(SignalName.DismissRequested);
	}
}