using Godot;

namespace BCSVRMuseum.Museum_Scripts.Decision;

public partial class DecisionPopup : DisplayActionPopup
{
	protected override Node FindPanel(Node sceneInstance)
	{
		return sceneInstance.FindChild("DecisionPanel", true, false);
	}

	protected override void BindPanel(Node panel)
	{
		var decisionPanel = (DecisionPanel)panel;
		decisionPanel.DismissRequested += Dismiss;
		decisionPanel.SimilaritySearchRequested += _ => RequestSimilaritySearch();
	}

	protected override void ApplyVectorToPanel(Node panel, string vectorJson)
	{
		((DecisionPanel)panel).SetVector(vectorJson);
	}
}
