using Godot;

namespace BCSVRMuseum.Museum_Scripts.Decision;

public partial class SimilarityPopup : DisplayActionPopup
{
	public override void _Ready()
	{
		if (PanelHostPath.ToString() is "" or "../2Din3DDecision")
			PanelHostPath = new NodePath("../2Din3DSimilarity");

		base._Ready();
	}

	protected override Node FindPanel(Node sceneInstance)
	{
		return sceneInstance.FindChild("SimilarityPanel", true, false);
	}

	protected override void BindPanel(Node panel)
	{
		((SimilarityPanel)panel).SimilaritySearchRequested += _ => RequestSimilaritySearch();
	}

	protected override void ApplyVectorToPanel(Node panel, string vectorJson)
	{
		((SimilarityPanel)panel).SetVector(vectorJson);
	}

}