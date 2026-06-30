using System;
using Godot;
namespace BCSVRMuseum.Museum_Scripts;

public partial class EnterSubmitTrigger : Node
{
	[Export] public NodePath ViewportPath;
	[Export] public NodePath Controller;
	[Export] public NodePath InputBridgePath = new("../InputBridge");

	private SearchController _submitter;
	private LineEdit _inputLineEdit;

	public override async void _Ready()
	{
		var viewport = GetNode<Viewport>(ViewportPath);
		var inputBridge = GetNode<InputBridge>(InputBridgePath);
		_submitter = GetNode<SearchController>(Controller);

		var enterKey = await this.WaitFor(() => FindNodeByName(viewport, "VirtualKeyEnter"), "enter key");
		_inputLineEdit = await this.WaitFor(() => inputBridge.InputLineEdit, "input line edit");
		enterKey.Connect("pressed", new Callable(this, nameof(OnEnterPressed)));
	}

	private static Node FindNodeByName(Node node, string name)
	{
		return FindNode(node, current => current.Name.ToString() == name);
	}

	private static Node FindNode(Node node, Func<Node, bool> matches)
	{
		if (matches(node))
			return node;

		foreach (var child in node.GetChildren())
		{
			var found = FindNode(child, matches);
			if (found != null)
				return found;
		}

		return null;
	}

	private void OnEnterPressed()
	{
		if (string.IsNullOrWhiteSpace(_inputLineEdit.Text))
			return;

		_submitter.SubmitText();
	}
}