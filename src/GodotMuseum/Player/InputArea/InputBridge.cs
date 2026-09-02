using BCSVRMuseum.Museum_Scripts;
using Godot;

namespace BCSVRMuseum.Player.InputArea;

/// <summary>
/// Makes a text field inside a viewport available to the main scene.
/// </summary>
public partial class InputBridge : Node
{
	[Export] public NodePath ViewportPath;

	public TextEdit InputTextEdit { get; private set; }

	/// <summary>
	/// Finds and stores the first text field in the viewport.
	/// </summary>
	public override async void _Ready()
	{
		var viewport = GetNode<Viewport>(ViewportPath);
		InputTextEdit = await this.WaitFor(() => FindFirstTextEdit(viewport), "input text edit");
	}

	/// <summary>
	/// Finds the first text field in a node and its children.
	/// </summary>
	/// <param name="node">The root node to search.</param>
	/// <returns>The first text field found or <see langword="null"/> when none exists.</returns>
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
