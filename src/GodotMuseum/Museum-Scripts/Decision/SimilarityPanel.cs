using Godot;

namespace BCSVRMuseum.Museum_Scripts.Decision;

public partial class SimilarityPanel : Node
{
	[Signal]
	public delegate void SimilaritySearchRequestedEventHandler(string vectorJson);

	private string _vectorJson = string.Empty;

	public override void _Ready()
	{
		GetNode<Button>("../Panel/SimilaritySearch").Pressed += OnSimilaritySearchPressed;
	}

	public void SetVector(string vectorJson)
	{
		_vectorJson = vectorJson;
	}

	private void OnSimilaritySearchPressed()
	{
		EmitSignal(SignalName.SimilaritySearchRequested, _vectorJson);
	}
}
