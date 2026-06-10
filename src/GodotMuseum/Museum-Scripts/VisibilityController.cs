using Godot;
namespace BCSVRMuseum.Museum_Scripts;

public partial class VisibilityController : Node
{
	[Export] public NodePath KeyboardPath;
	[Export] public NodePath OutputFramePath;

	private Node3D _keyboard;
	private Node3D _outputFrame;

	public override void _Ready()
	{
		_keyboard = GetNode<Node3D>(KeyboardPath);
		_outputFrame = GetNode<Node3D>(OutputFramePath);

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
			return;

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
