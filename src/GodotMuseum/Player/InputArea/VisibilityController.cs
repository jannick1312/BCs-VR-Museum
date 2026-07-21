using BCSVRMuseum.Museum_Scripts;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using BCSVRMuseum.Museum_Scripts.Placement.Object3D;
using Godot;

namespace BCSVRMuseum.Player.InputArea;

public partial class VisibilityController : Node
{
	private const float GripPressThreshold = 0.8f;
	private const float GripReleaseThreshold = 0.6f;

	private bool _goBackActive;
	private bool _gripPressed;
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
		UpdateGripState(_leftController.GetFloat("grip"));
		var inOriginalSizeRoom = _museumNode.Visible && _originalSizeController.IsInOriginalSizeRoom;

		SetInputActive(_museumNode.Visible && !inOriginalSizeRoom && _gripPressed);
		SetGoBackActive(inOriginalSizeRoom && _gripPressed);
	}

	private void UpdateGripState(float gripValue)
	{
		if (_gripPressed)
		{
			if (gripValue < GripReleaseThreshold)
				_gripPressed = false;
		}
		else if (gripValue > GripPressThreshold)
		{
			_gripPressed = true;
		}
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
