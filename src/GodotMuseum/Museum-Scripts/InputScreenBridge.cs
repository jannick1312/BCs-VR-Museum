using Godot;
namespace BCSVRMuseum.Museum_Scripts;

public partial class InputScreenBridge : Node
{
	[Export] public NodePath ViewportPath;

	public LineEdit InputLineEdit { get; private set; }

	public override async void _Ready()
	{
		for (var i = 0; i < 8; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		var viewport = GetNodeOrNull<Viewport>(ViewportPath);

		InputLineEdit = FindFirstLineEdit(viewport);
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
