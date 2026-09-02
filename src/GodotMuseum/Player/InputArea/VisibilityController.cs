using BCSVRMuseum.Museum_Scripts;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using BCSVRMuseum.Museum_Scripts.Placement.Object3D;
using Godot;

namespace BCSVRMuseum.Player.InputArea;

/// <summary>
/// Manages visibility of controls on the left hand.
/// </summary>
public partial class VisibilityController : Node
{
	private bool _goBackActive;
	private Node _goBackRoot;
	private bool _inputActive;
	private Node _inputRoot;
	private Node _leftPickup;
	private Node3D _museumNode;
	private OriginalSizeController _originalSizeController;

	[Export] public NodePath GoBackRootPath;
	[Export] public NodePath InputRootPath;
	[Export] public NodePath LeftPickupPath;
	[Export] public NodePath MuseumNodePath;
	[Export] public NodePath OriginalSizeControllerPath;

	/// <summary>
	/// Finds the required nodes and connects the return button.
	/// </summary>
	public override async void _Ready()
	{
		_goBackRoot = GetNode(GoBackRootPath);
		_inputRoot = GetNode(InputRootPath);
		_leftPickup = GetNode(LeftPickupPath);
		_museumNode = GetNode<Node3D>(MuseumNodePath);
		_originalSizeController = GetNode<OriginalSizeController>(OriginalSizeControllerPath);

		NodeTreeActivator.SetActive(_goBackRoot, false);
		NodeTreeActivator.SetActive(_inputRoot, false);
		_goBackActive = false;
		_inputActive = false;

		var museumButton = await this.WaitFor(
			() => _goBackRoot.FindChild("Museum", true, false) as Button,
			"go-back museum button");
		if (_originalSizeController != null)
			museumButton.Pressed += _originalSizeController.ReturnToMuseum;
	}

	/// <summary>
	/// Shows the search or return control while the left grip is pressed.
	/// </summary>
	/// <param name="delta">The frame time in seconds.</param>
	public override void _Process(double delta)
	{
		var gripPressed = _leftPickup.Get("grip_pressed").AsBool();
		var inOriginalSizeRoom = _museumNode.Visible && (_originalSizeController?.IsInOriginalSizeRoom ?? false);

		SetInputActive(_museumNode.Visible && !inOriginalSizeRoom && gripPressed);
		SetGoBackActive(inOriginalSizeRoom && gripPressed);
	}

	/// <summary>
	/// Sets the return control as active or inactive.
	/// </summary>
	/// <param name="active">If the return control should be active.</param>
	private void SetGoBackActive(bool active)
	{
		if (_goBackActive == active)
			return;

		_goBackActive = active;
		NodeTreeActivator.SetActive(_goBackRoot, active);
	}

	/// <summary>
	/// Sets the search input as active or inactive.
	/// </summary>
	/// <param name="active">If the search input should be active.</param>
	private void SetInputActive(bool active)
	{
		if (_inputActive == active)
			return;

		_inputActive = active;
		NodeTreeActivator.SetActive(_inputRoot, active);
	}
}
