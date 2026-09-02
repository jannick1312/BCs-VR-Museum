using Godot;

namespace BCSVRMuseum.Museum_Scripts.Decision;

/// <summary>
/// Shows original-size and similarity-search buttons for media.
/// </summary>
public partial class DecisionPanel : Node
{
	/// <summary>
	/// Defines a signal for closing the decision panel.
	/// </summary>
	[Signal]
	public delegate void DismissRequestedEventHandler();

	/// <summary>
	/// Defines a signal for showing the selected 3D model at original size.
	/// </summary>
	[Signal]
	public delegate void OriginalSizeRequestedEventHandler();

	/// <summary>
	/// Defines a signal for searching for media similar to the selected item.
	/// </summary>
	/// <param name="vectorJson">The stored feature vector.</param>
	[Signal]
	public delegate void SimilaritySearchRequestedEventHandler(string vectorJson);

	private string _vectorJson = string.Empty;

	/// <summary>
	/// Connects the panel's buttons.
	/// </summary>
	public override void _Ready()
	{
		GetNode<Button>("../Panel/OriginalSize").Pressed += OnOriginalSizePressed;
		GetNode<Button>("../Panel/SimilaritySearch").Pressed += OnSimilaritySearchPressed;
	}

	/// <summary>
	/// Stores the feature vector used for a similarity search.
	/// </summary>
	/// <param name="vectorJson">The stored feature vector.</param>
	public void SetVector(string vectorJson)
	{
		_vectorJson = vectorJson;
	}

	/// <summary>
	/// Requests original-size display and closes the panel.
	/// </summary>
	private void OnOriginalSizePressed()
	{
		EmitSignal(SignalName.OriginalSizeRequested);
		EmitSignal(SignalName.DismissRequested);
	}

	/// <summary>
	/// Requests a similarity search and closes the panel.
	/// </summary>
	private void OnSimilaritySearchPressed()
	{
		EmitSignal(SignalName.SimilaritySearchRequested, _vectorJson);
		EmitSignal(SignalName.DismissRequested);
	}
}
