using Godot;

namespace BCSVRMuseum.Keyboard;

public partial class VirtualToggleKey : Button
{
	public override void _Ready()
	{
		FocusMode = FocusModeEnum.None;
		ToggleMode = true;
	}
}
