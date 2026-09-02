using Godot;

namespace BCSVRMuseum.Museum_Scripts.Decision;

/// <summary>
/// Connects a similarity panel to a media item.
/// </summary>
public partial class SimilarityPopup : DisplayActionPopup
{
	/// <summary>
	/// Finds the similarity panel inside the scene.
	/// </summary>
	/// <param name="sceneInstance">The panel scene.</param>
	/// <returns>The similarity panel.</returns>
	protected override Node FindPanel(Node sceneInstance)
	{
		return sceneInstance.FindChild("SimilarityPanel", true, false);
	}

	/// <summary>
	/// Connects the search button from the similarity panel.
	/// </summary>
	/// <param name="panel">The similarity panel to connect.</param>
	protected override void BindPanel(Node panel)
	{
		var similarityPanel = (SimilarityPanel)panel;
		similarityPanel.SimilaritySearchRequested += RequestSimilaritySearch;
	}

	/// <summary>
	/// Sets the feature vector on the similarity panel.
	/// </summary>
	/// <param name="panel">The similarity panel to update.</param>
	/// <param name="vectorJson">The stored feature vector.</param>
	protected override void ApplyVectorToPanel(Node panel, string vectorJson)
	{
		((SimilarityPanel)panel).SetVector(vectorJson);
	}
}
