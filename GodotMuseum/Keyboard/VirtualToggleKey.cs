using Godot;

public partial class VirtualToggleKey : Button
{
	public override void _Ready()
	{
		FocusMode = FocusModeEnum.None;
		ToggleMode = true;
	}
}
