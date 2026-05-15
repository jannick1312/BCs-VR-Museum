using Godot;

public partial class InputScreenBridge : Node
{
	[Export] public NodePath ViewportPath;

	public LineEdit InputLineEdit { get; private set; }

	public override async void _Ready()
	{
		for (int i = 0; i < 8; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		Viewport viewport = GetNodeOrNull<Viewport>(ViewportPath);

		if (viewport == null)
		{
			GD.PrintErr("InputScreenBridge: ViewportPath ist falsch.");
			return;
		}

		InputLineEdit = FindFirstLineEdit(viewport);

		if (InputLineEdit == null)
			GD.PrintErr("InputScreenBridge: Kein LineEdit im Viewport gefunden.");
	}

	private LineEdit FindFirstLineEdit(Node node)
	{
		if (node == null)
			return null;

		if (node is LineEdit lineEdit)
			return lineEdit;

		foreach (Node child in node.GetChildren())
		{
			LineEdit found = FindFirstLineEdit(child);

			if (found != null)
				return found;
		}

		return null;
	}
}
