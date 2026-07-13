using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using Godot;

namespace BCSVRMuseum.Player.InputArea;

public partial class VisibilityController : Node
{
	private bool _inputActive;
	private Node _inputRoot;
	private Node _leftPickup;
	private Node3D _museumNode;

	[Export] public NodePath InputRootPath;
	[Export] public NodePath LeftPickupPath;
	[Export] public NodePath MuseumNodePath;

	public override void _Ready()
	{
		_inputRoot = GetNode(InputRootPath);
		_leftPickup = GetNode(LeftPickupPath);
		_museumNode = GetNode<Node3D>(MuseumNodePath);

		NodeTreeActivator.SetActive(_inputRoot, false);
		_inputActive = false;
	}

	public override void _Process(double delta)
	{
		SetInputActive(_museumNode.Visible && _leftPickup.Get("grip_pressed").AsBool());
	}

	private void SetInputActive(bool active)
	{
		if (_inputActive == active)
			return;

		_inputActive = active;
		NodeTreeActivator.SetActive(_inputRoot, active);
	}
}
