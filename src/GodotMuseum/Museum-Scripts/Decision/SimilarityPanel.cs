using Godot;

namespace BCSVRMuseum.Museum_Scripts.Decision;

/// <summary>
/// Shows the similarity-search button for media.
/// </summary>
public partial class SimilarityPanel : Node
{
	/// <summary>
	/// Defines a signal for requesting a similarity search.
	/// </summary>
	/// <param name="vectorJson">The stored feature vector.</param>
	[Signal]
	public delegate void SimilaritySearchRequestedEventHandler(string vectorJson);

	private string _vectorJson = string.Empty;

	/// <summary>
	/// Connects the similarity-search button.
	/// </summary>
	public override void _Ready()
	{
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
	/// Requests a similarity search for the stored vector.
	/// </summary>
	private void OnSimilaritySearchPressed()
	{
		EmitSignal(SignalName.SimilaritySearchRequested, _vectorJson);
	}
}
