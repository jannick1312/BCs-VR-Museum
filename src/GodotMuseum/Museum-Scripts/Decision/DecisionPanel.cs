using Godot;

namespace BCSVRMuseum.Museum_Scripts.Decision;

public partial class DecisionPanel : Node
{
	[Signal]
	public delegate void DismissRequestedEventHandler();

	[Signal]
	public delegate void OriginalSizeRequestedEventHandler();

	[Signal]
	public delegate void SimilaritySearchRequestedEventHandler(string vectorJson);

	private string _vectorJson = string.Empty;

	public override void _Ready()
	{
		GetNode<Button>("../Panel/OriginalSize").Pressed += OnOriginalSizePressed;
		GetNode<Button>("../Panel/SimilaritySearch").Pressed += OnSimilaritySearchPressed;
	}

	public void SetVector(string vectorJson)
	{
		_vectorJson = vectorJson;
	}

	private void OnOriginalSizePressed()
	{
		EmitSignal(SignalName.OriginalSizeRequested);
		EmitSignal(SignalName.DismissRequested);
	}

	private void OnSimilaritySearchPressed()
	{
		EmitSignal(SignalName.SimilaritySearchRequested, _vectorJson);
		EmitSignal(SignalName.DismissRequested);
	}
}
