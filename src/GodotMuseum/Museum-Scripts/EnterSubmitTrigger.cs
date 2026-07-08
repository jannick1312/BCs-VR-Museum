using Godot;
namespace BCSVRMuseum.Museum_Scripts;

public partial class EnterSubmitTrigger : Node
{
	[Export] public NodePath ViewportPath;
	[Export] public NodePath Controller;
	[Export] public NodePath InputBridgePath;

	private SearchController _submitter;
	private LineEdit _inputLineEdit;

	public override async void _Ready()
	{
		var viewport = GetNode<Viewport>(ViewportPath);
		var inputBridge = GetNode<Player.InputArea.InputBridge>(InputBridgePath);
		_submitter = GetNode<SearchController>(Controller);

		var enterKey = await this.WaitFor(() => viewport.FindChild("VirtualKeyEnter", true, false), "enter key");
		_inputLineEdit = await this.WaitFor(() => inputBridge.InputLineEdit, "input line edit");
		enterKey.Connect("pressed", new Callable(this, nameof(OnEnterPressed)));
	}

	private void OnEnterPressed()
	{
		if (string.IsNullOrWhiteSpace(_inputLineEdit.Text))
			return;

		_submitter.SubmitText();
	}
}