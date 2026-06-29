using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts;

public partial class VisibilityController : Node
{
	private static readonly EventLogger Logger = new(nameof(VisibilityController));

	[Export] public NodePath KeyboardPath;
	[Export] public NodePath OutputFramePath;

	private Node3D _keyboard;
	private Node3D _outputFrame;

	public override void _Ready()
	{
		_keyboard = GetNodeOrNull<Node3D>(KeyboardPath);
		_outputFrame = GetNodeOrNull<Node3D>(OutputFramePath);

		if (_keyboard == null)
			Logger.Warning("Keyboard node is missing.");

		if (_outputFrame == null)
			Logger.Warning("Output frame node is missing.");

		HideKeyboard();
		HideOutput();
	}

	public void ShowKeyboard()
	{
		SetTreeActive(_keyboard, true);
		SetTreeActive(_outputFrame, false);
	}

	public void HideKeyboard()
	{
		SetTreeActive(_keyboard, false);
	}

	public void ShowOutput()
	{
		SetTreeActive(_outputFrame, true);
	}

	private void HideOutput()
	{
		SetTreeActive(_outputFrame, false);
	}

	private static void SetTreeActive(Node node, bool active)
	{
		if (node is null)
		{
			Logger.Warning($"Cannot set active={active} because node is null.");
			return;
		}

		if (node is Node3D node3D)
			node3D.Visible = active;

		if (node is CollisionShape3D collisionShape)
			collisionShape.SetDeferred(CollisionShape3D.PropertyName.Disabled, !active);

		if (node is Viewport viewport)
			viewport.SetProcessInput(active);

		foreach (var child in node.GetChildren())
			SetTreeActive(child, active);
	}
}