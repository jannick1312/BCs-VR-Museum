using Godot;

namespace BCSVRMuseum.Keyboard;

public partial class VirtualKeyInputEvent : Button
{
	[Signal]
	public delegate void KeyPressedEventHandler(string scanCodeText, int unicode, bool shift);

	[Export] public string ScanCodeText;
	[Export] public bool ShiftPressed;
	[Export] public int Unicode;

	public override void _Ready()
	{
		FocusMode = FocusModeEnum.None;
		Pressed += OnPressed;
	}

	private void OnPressed()
	{
		EmitSignal(SignalName.KeyPressed, ScanCodeText, Unicode, ShiftPressed);
	}
}
