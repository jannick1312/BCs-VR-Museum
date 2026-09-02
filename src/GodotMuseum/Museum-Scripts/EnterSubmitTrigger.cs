using BCSVRMuseum.Player.InputArea;
using Godot;

namespace BCSVRMuseum.Museum_Scripts;

/// <summary>
/// Submits museum text input when the virtual Enter key is pressed.
/// </summary>
public partial class EnterSubmitTrigger : Node
{
	private CenteredTextController _inputController;
	private TextEdit _inputTextEdit;
	private SearchController _submitter;

	[Export] public NodePath Controller;
	[Export] public NodePath InputBridgePath;
	[Export] public NodePath ViewportPath;

	/// <summary>
	/// Finds the text input and connects the virtual Enter key.
	/// </summary>
	public override async void _Ready()
	{
		var viewport = GetNode<Viewport>(ViewportPath);
		var inputBridge = GetNode<InputBridge>(InputBridgePath);
		_submitter = GetNode<SearchController>(Controller);

		var enterKey = await this.WaitFor(() => viewport.FindChild("VirtualKeyEnter", true, false), "enter key");
		_inputTextEdit = await this.WaitFor(() => inputBridge.InputTextEdit, "input text edit");
		_inputController = _inputTextEdit.GetParent().GetNode<CenteredTextController>("CenteredTextController");
		enterKey.Connect("pressed", new Callable(this, nameof(OnEnterPressed)));
	}

	/// <summary>
	/// Submits non-empty text and resets the input display.
	/// </summary>
	private void OnEnterPressed()
	{
		if (!string.IsNullOrWhiteSpace(_inputTextEdit.Text))
			_submitter.SubmitText();

		Callable.From(_inputController.ResetInput).CallDeferred();
	}
}
