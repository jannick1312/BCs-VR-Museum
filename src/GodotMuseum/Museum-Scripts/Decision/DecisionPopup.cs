using Godot;

namespace BCSVRMuseum.Museum_Scripts.Decision;

/// <summary>
/// Connects a decision panel to a media item.
/// </summary>
public partial class DecisionPopup : DisplayActionPopup
{
	/// <summary>
	/// Finds the decision panel inside the scene.
	/// </summary>
	/// <param name="sceneInstance">The panel scene.</param>
	/// <returns>The decision panel.</returns>
	protected override Node FindPanel(Node sceneInstance)
	{
		return sceneInstance.FindChild("DecisionPanel", true, false);
	}

	/// <summary>
	/// Connects the buttons from the decision panel.
	/// </summary>
	/// <param name="panel">The decision panel to connect.</param>
	protected override void BindPanel(Node panel)
	{
		var decisionPanel = (DecisionPanel)panel;
		decisionPanel.DismissRequested += Dismiss;
		decisionPanel.OriginalSizeRequested += RequestOriginalSize;
		decisionPanel.SimilaritySearchRequested += RequestSimilaritySearch;
	}

	/// <summary>
	/// Sets the feature vector on the decision panel.
	/// </summary>
	/// <param name="panel">The decision panel to update.</param>
	/// <param name="vectorJson">The stored feature vector.</param>
	protected override void ApplyVectorToPanel(Node panel, string vectorJson)
	{
		((DecisionPanel)panel).SetVector(vectorJson);
	}
}
