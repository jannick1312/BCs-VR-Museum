using Godot;

namespace BCSVRMuseum.Keyboard;

/// <summary>
/// Sends a virtual key event when pressed.
/// </summary>
public partial class VirtualKeyInputEvent : Button
{
	/// <summary>
	/// Defines a signal for a virtual key press.
	/// </summary>
	/// <param name="scanCodeText">The key name used to find the scan code.</param>
	/// <param name="unicode">The Unicode value produced by the key.</param>
	/// <param name="shift">If the key includes the Shift modifier.</param>
	[Signal]
	public delegate void KeyPressedEventHandler(string scanCodeText, int unicode, bool shift);

	[Export] public string ScanCodeText;
	[Export] public bool ShiftPressed;
	[Export] public int Unicode;

	/// <summary>
	/// Disables focus and connects the button press handler.
	/// </summary>
	public override void _Ready()
	{
		FocusMode = FocusModeEnum.None;
		Pressed += OnPressed;
	}

	/// <summary>
	/// Sends the set scan code, Unicode value, and Shift state.
	/// </summary>
	private void OnPressed()
	{
		EmitSignal(SignalName.KeyPressed, ScanCodeText, Unicode, ShiftPressed);
	}
}



// This keyboard is based on the keyboard from the Godot XR Tools add-on. Codex helped adapt it to C# as a starting point for this project.
