using Godot;

public partial class EnterSubmitTrigger : Node
{
	public override async void _Ready()
	{
		for (int i = 0; i < 8; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		var viewport = GetNode<Viewport>("../Viewport");

		Node enterKey = FindNodeByName(viewport, "VirtualKeyEnter");

		enterKey.Connect(
			"pressed",
			new Callable(this, nameof(OnEnterPressed))
		);
	}

	private Node FindNodeByName(Node node, string name)
	{
		if (node.Name.ToString() == name)
			return node;

		foreach (Node child in node.GetChildren())
		{
			Node found = FindNodeByName(child, name);
			if (found != null)
				return found;
		}

		return null;
	}

	private void OnEnterPressed()
	{

		var submitter = GetNodeOrNull<KeyboardSubmitter>("../KeyboardSubmitter");

		submitter.SubmitText();
	}
}
