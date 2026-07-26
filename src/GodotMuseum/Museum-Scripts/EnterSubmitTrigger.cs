using BCSVRMuseum.Player.InputArea;
using Godot;

namespace BCSVRMuseum.Museum_Scripts;

public partial class EnterSubmitTrigger : Node
{
	private TextEdit _inputTextEdit;
	private SearchController _submitter;

	[Export] public NodePath Controller;
	[Export] public NodePath InputBridgePath;
	[Export] public NodePath ViewportPath;

	public override async void _Ready()
	{
		var viewport = GetNode<Viewport>(ViewportPath);
		var inputBridge = GetNode<InputBridge>(InputBridgePath);
		_submitter = GetNode<SearchController>(Controller);

		var enterKey = await this.WaitFor(() => viewport.FindChild("VirtualKeyEnter", true, false), "enter key");
		_inputTextEdit = await this.WaitFor(() => inputBridge.InputTextEdit, "input text edit");
		enterKey.Connect("pressed", new Callable(this, nameof(OnEnterPressed)));
	}

	private void OnEnterPressed()
	{
		if (string.IsNullOrWhiteSpace(_inputTextEdit.Text))
			return;

		_submitter.SubmitText();
	}
}
