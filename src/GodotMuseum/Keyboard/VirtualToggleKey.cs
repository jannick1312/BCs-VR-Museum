using Godot;

namespace BCSVRMuseum.Keyboard;

/// <summary>
/// Configures a virtual keyboard button as a focus-free toggle.
/// </summary>
public partial class VirtualToggleKey : Button
{
	/// <summary>
	/// Enables toggle behavior and disables keyboard focus.
	/// </summary>
	public override void _Ready()
	{
		FocusMode = FocusModeEnum.None;
		ToggleMode = true;
	}
}



// This keyboard is based on the keyboard from the Godot XR Tools add-on. Codex helped adapt it to C# as a starting point for this project.
