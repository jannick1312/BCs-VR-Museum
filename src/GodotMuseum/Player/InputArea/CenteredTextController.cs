using Godot;

namespace BCSVRMuseum.Player.InputArea;

/// <summary>
/// Mirrors editable text into a centered display.
/// </summary>
public partial class CenteredTextController : Node
{
	private Label _displayText;
	private Label _placeholderText;
	private TextEdit _textEdit;

	[Export] public NodePath DisplayTextPath;
	[Export] public NodePath PlaceholderTextPath;
	[Export] public NodePath TextEditPath;

	/// <summary>
	/// Finds text controls and keeps their text in sync.
	/// </summary>
	public override void _Ready()
	{
		_textEdit = GetNode<TextEdit>(TextEditPath);
		_displayText = GetNode<Label>(DisplayTextPath);
		_placeholderText = GetNode<Label>(PlaceholderTextPath);

		_textEdit.TextChanged += SyncDisplay;
		SyncDisplay();
	}

	/// <summary>
	/// Stops syncing text changes.
	/// </summary>
	public override void _ExitTree()
	{
		_textEdit?.TextChanged -= SyncDisplay;
	}

	/// <summary>
	/// Resets the input display.
	/// </summary>
	public void ResetInput()
	{
		_textEdit.Text = string.Empty;
		_textEdit.ReleaseFocus();
		SyncDisplay();
	}

	/// <summary>
	/// Copies the input text and shows the hint when the input is empty.
	/// </summary>
	private void SyncDisplay()
	{
		_displayText.Text = _textEdit.Text;
		_placeholderText.Visible = string.IsNullOrWhiteSpace(_textEdit.Text);
	}
}



// Codex helped adapt the standard TextEdit into a centered input display where long text is split across multiple lines.
