using Godot;

namespace BCSVRMuseum.Museum_Scripts.Decision;

public partial class SimilarityPopup : DisplayActionPopup
{
	protected override Node FindPanel(Node sceneInstance)
	{
		return sceneInstance.FindChild("SimilarityPanel", true, false);
	}

	protected override void BindPanel(Node panel)
	{
		var similarityPanel = (SimilarityPanel)panel;
		similarityPanel.SimilaritySearchRequested += RequestSimilaritySearch;
	}

	protected override void ApplyVectorToPanel(Node panel, string vectorJson)
	{
		((SimilarityPanel)panel).SetVector(vectorJson);
	}
}
