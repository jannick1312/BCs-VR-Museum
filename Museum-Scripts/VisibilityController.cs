using Godot;

public partial class VisibilityController : Node
{
	[Export] public NodePath PickableKeyboardPath;
	[Export] public NodePath OutputFramePath;

	private Node3D _keyboardPickable;
	private Node3D _outputFrame;

	public override void _Ready()
	{
		_keyboardPickable = GetNode<Node3D>(PickableKeyboardPath);
		_outputFrame = GetNode<Node3D>(OutputFramePath);

		HideKeyboard();
		HideOutput();
	}

	public void ShowKeyboard()
	{
		SetTreeVisible(_keyboardPickable, true);
		HideOutput();
	}

	public void HideKeyboard()
	{
		SetTreeVisible(_keyboardPickable, false);
	}

	public void ShowOutput()
	{
		SetTreeVisible(_outputFrame, true);
	}

	public void HideOutput()
	{
		SetTreeVisible(_outputFrame, false);
	}

	private void SetTreeVisible(Node node, bool visible)
	{
		if (node is Node3D node3D)
			node3D.Visible = visible;

		if (node is CollisionShape3D collisionShape)
			collisionShape.Disabled = !visible;

		if (node is Viewport viewport)
			viewport.SetProcessInput(visible);

		foreach (Node child in node.GetChildren())
			SetTreeVisible(child, visible);
	}
}
