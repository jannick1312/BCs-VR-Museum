using Godot;
namespace BCSVRMuseum.Museum_Scripts;

public partial class InputBridge : Node
{
	[Export] public NodePath ViewportPath;

	public LineEdit InputLineEdit { get; private set; }

	public override async void _Ready()
	{
		var viewport = GetNode<Viewport>(ViewportPath);
		InputLineEdit = await this.WaitFor(() => FindFirstLineEdit(viewport), "input line edit");
	}

	private static LineEdit FindFirstLineEdit(Node node)
	{
		if (node is null or LineEdit)
			return (LineEdit)node;

		foreach (var child in node.GetChildren())
		{
			var found = FindFirstLineEdit(child);

			if (found != null)
				return found;
		}

		return null;
	}
}
