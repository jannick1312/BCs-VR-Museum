using Godot;

public partial class VisibilityController : Node
{
	[Export] public NodePath PickableKeyboardPath;
	[Export] public NodePath PickableOutputPath;

	private Node3D _keyboardPickable;
	private Node3D _outputPickable;

	public override void _Ready()
	{
		_keyboardPickable = GetNode<Node3D>(PickableKeyboardPath);
		_outputPickable = GetNode<Node3D>(PickableOutputPath);

		HideKeyboard();
		HideOutput();
	}

	public void ShowKeyboard()
	{
		SetPickableVisible(_keyboardPickable, true);
		HideOutput();
	}

	public void HideKeyboard()
	{
		SetPickableVisible(_keyboardPickable, false);
	}

	public void ShowOutput()
	{
		SetPickableVisible(_outputPickable, true);
	}

	public void HideOutput()
	{
		SetPickableVisible(_outputPickable, false);
	}

	private void SetPickableVisible(Node node, bool visible)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is MeshInstance3D mesh)
				mesh.Visible = visible;

			if (child is CollisionShape3D collisionShape)
				collisionShape.Disabled = !visible;

			if (child.Name == "VirtualKeyboard" || child.Name == "OutputScreen")
				SetScreenVisible(child, visible);
		}
	}

	private void SetScreenVisible(Node screenRoot, bool visible)
	{
		var screenMesh = screenRoot.GetNodeOrNull<Node3D>("Screen");
		if (screenMesh != null)
			screenMesh.Visible = visible;

		var collision = screenRoot.GetNodeOrNull<CollisionShape3D>("StaticBody3D/CollisionShape3D");
		if (collision != null)
			collision.Disabled = !visible;

		var viewport = screenRoot.GetNodeOrNull<Viewport>("Viewport");
		if (viewport != null)
			viewport.SetProcessInput(visible);
	}
}
