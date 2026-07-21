using BCSVRMuseum.Museum_Scripts;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using BCSVRMuseum.Museum_Scripts.Placement.Object3D;
using Godot;

namespace BCSVRMuseum.Player.InputArea;

public partial class VisibilityController : Node
{
	private bool _goBackActive;
	private Node _goBackRoot;
	private bool _inputActive;
	private Node _inputRoot;
	private XRController3D _leftController;
	private Node _leftPickup;
	private Node3D _museumNode;
	private OriginalSizeController _originalSizeController;

	[Export] public NodePath GoBackRootPath;
	[Export] public NodePath InputRootPath;
	[Export] public NodePath LeftPickupPath;
	[Export] public NodePath MuseumNodePath;
	[Export] public NodePath OriginalSizeControllerPath;

	public override async void _Ready()
	{
		_goBackRoot = GetNode(GoBackRootPath);
		_inputRoot = GetNode(InputRootPath);
		_leftPickup = GetNode(LeftPickupPath);
		_leftController = _leftPickup.GetParent().GetParent<XRController3D>();
		_museumNode = GetNode<Node3D>(MuseumNodePath);
		_originalSizeController = GetNode<OriginalSizeController>(OriginalSizeControllerPath);

		NodeTreeActivator.SetActive(_goBackRoot, false);
		NodeTreeActivator.SetActive(_inputRoot, false);
		_goBackActive = false;
		_inputActive = false;

		var museumButton = await this.WaitFor(
			() => _goBackRoot.FindChild("Museum", true, false) as Button,
			"go-back museum button");
		museumButton.Pressed += _originalSizeController.ReturnToMuseum;
	}

	public override void _Process(double delta)
	{
		var gripPressed = _leftController.GetIsActive() && _leftController.GetFloat("grip") > 0.6f;
		var inOriginalSizeRoom = _museumNode.Visible && _originalSizeController.IsInOriginalSizeRoom;

		SetInputActive(_museumNode.Visible && !inOriginalSizeRoom && gripPressed);
		SetGoBackActive(inOriginalSizeRoom && gripPressed);
	}

	private void SetGoBackActive(bool active)
	{
		if (_goBackActive == active)
			return;

		_goBackActive = active;
		NodeTreeActivator.SetActive(_goBackRoot, active);
	}

	private void SetInputActive(bool active)
	{
		if (_inputActive == active)
			return;

		_inputActive = active;
		NodeTreeActivator.SetActive(_inputRoot, active);
	}
}
