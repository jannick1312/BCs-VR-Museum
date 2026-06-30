using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using Godot;

namespace BCSVRMuseum.Museum_Scripts;

public partial class VisibilityController : Node
{
	[Export] public NodePath KeyboardPath;

	private Node3D _keyboard;

	public override void _Ready()
	{
		_keyboard = GetNode<Node3D>(KeyboardPath);
		HideKeyboard();
	}

	public void ShowKeyboard()
	{
		NodeTreeActivator.SetActive(_keyboard, true);
	}

	public void HideKeyboard()
	{
		NodeTreeActivator.SetActive(_keyboard, false);
	}
}