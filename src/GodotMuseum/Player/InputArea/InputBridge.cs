using BCSVRMuseum.Museum_Scripts;
using Godot;

namespace BCSVRMuseum.Player.InputArea;

public partial class InputBridge : Node
{
	[Export] public NodePath ViewportPath;

	public TextEdit InputTextEdit { get; private set; }

	public override async void _Ready()
	{
		var viewport = GetNode<Viewport>(ViewportPath);
		InputTextEdit = await this.WaitFor(() => FindFirstTextEdit(viewport), "input text edit");
	}

	private static TextEdit FindFirstTextEdit(Node node)
	{
		if (node is TextEdit textEdit)
			return textEdit;

		foreach (var child in node.GetChildren())
		{
			var found = FindFirstTextEdit(child);

			if (found != null)
				return found;
		}

		return null;
	}
}
