using Godot;
namespace BCSVRMuseum.Museum_Scripts;

public partial class EnterSubmitTrigger : Node
{
	[Export] public NodePath ViewportPath;
	[Export] public NodePath Controller;

	private SearchController _submitter;

	public override async void _Ready()
	{
		for (var i = 0; i < 8; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		var viewport = GetNodeOrNull<Viewport>(ViewportPath);
		_submitter = GetNodeOrNull<SearchController>(Controller);

		var enterKey = FindNodeByName(viewport, "VirtualKeyEnter");
		enterKey.Connect("pressed", new Callable(this, nameof(OnEnterPressed)));
	}

	private static Node FindNodeByName(Node node, string name)
	{
		if (node.Name.ToString() == name)
			return node;

		foreach (var child in node.GetChildren())
		{
			var found = FindNodeByName(child, name);
			if (found != null)
				return found;
		}

		return null;
	}

	private void OnEnterPressed()
	{
		_submitter.SubmitText();
	}
}
