using Godot;

namespace BCSVRMuseum.Player.InputArea;

public partial class CenteredTextController : Node
{
	private Label _displayText;
	private Label _placeholderText;
	private TextEdit _textEdit;

	[Export] public NodePath DisplayTextPath;
	[Export] public NodePath PlaceholderTextPath;
	[Export] public NodePath TextEditPath;

	public override void _Ready()
	{
		_textEdit = GetNode<TextEdit>(TextEditPath);
		_displayText = GetNode<Label>(DisplayTextPath);
		_placeholderText = GetNode<Label>(PlaceholderTextPath);

		_textEdit.TextChanged += SyncDisplay;
		SyncDisplay();
	}

	public override void _ExitTree()
	{
		_textEdit?.TextChanged -= SyncDisplay;
	}

	public void ResetInput()
	{
		_textEdit.Text = string.Empty;
		_textEdit.ReleaseFocus();
		SyncDisplay();
	}

	private void SyncDisplay()
	{
		_displayText.Text = _textEdit.Text;
		_placeholderText.Visible = string.IsNullOrWhiteSpace(_textEdit.Text);
	}
}
