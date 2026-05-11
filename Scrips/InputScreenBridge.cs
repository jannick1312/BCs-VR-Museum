using Godot;

public partial class InputScreenBridge : Node
{
	public LineEdit InputLineEdit { get; private set; }

	public override async void _Ready()
	{
		for (int i = 0; i < 4; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		var viewport = GetNodeOrNull<Viewport>("../InputScreen/Viewport");

		InputLineEdit = FindFirstLineEdit(viewport);
	}

	private LineEdit FindFirstLineEdit(Node node)
	{
		if (node is LineEdit lineEdit)
			return lineEdit;

		foreach (Node child in node.GetChildren())
		{
			var found = FindFirstLineEdit(child);
			if (found != null)
				return found;
		}

		return null;
	}
}
