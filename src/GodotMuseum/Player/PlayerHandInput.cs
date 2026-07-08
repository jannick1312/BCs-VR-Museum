using Godot;

namespace BCSVRMuseum.Player;

public partial class PlayerHandInput : Node
{
	[Export] public StringName PinchAction { get; set; } = "trigger";
	[Export] public StringName GripAction { get; set; } = "grip";
	[Export] public StringName MovementAction { get; set; } = "primary";
	[Export] public float PinchThreshold { get; set; } = 0.5f;
	[Export] public float PinchReleaseThreshold { get; set; } = 0.35f;
	[Export] public float GrabThreshold { get; set; } = 0.88f;
	[Export] public float GrabReleaseThreshold { get; set; } = 0.68f;
	[Export] public float LeftPinchMoveDelaySeconds { get; set; } = 0.4f;
	[Export] public float ControllerInputActiveThreshold { get; set; } = 0.15f;

	private Node3D _player;
	private Node _playerBody;
	private Node3D _leftTrackedHand;
	private Node3D _rightTrackedHand;
	private XRController3D _leftController;
	private XRController3D _rightController;
	private HandJoystickMovement _leftMovement;
	private PlayerInputModeDetector _modeDetector;
	private PlayerInputVisuals _visuals;
	private HandGestureInput _gestures;
	private bool _leftHandTrackingActive;
	private bool _rightHandTrackingActive;
	private PlayerInputMode? _activeVisualMode;

	public override void _Ready()
	{
		_player = FindPlayer();
		_playerBody = _player.FindChild("PlayerBody", true, false);
		_leftTrackedHand = (Node3D)_player.FindChild("LeftTrackedHand", true, false);
		_rightTrackedHand = (Node3D)_player.FindChild("RightTrackedHand", true, false);
		_leftController = (XRController3D)_player.FindChild("LeftController", true, false);
		_rightController = (XRController3D)_player.FindChild("RightController", true, false);
		_leftMovement = (HandJoystickMovement)_player.FindChild("Movement", true, false);
		_leftMovement.Configure(_player);

		_modeDetector = new PlayerInputModeDetector(_player);
		_visuals = new PlayerInputVisuals(_player);
		_gestures = new HandGestureInput(_player, _leftMovement, this);

		_gestures.Reset();
	}

	private Node3D FindPlayer()
	{
		var node = GetParent();
		while (node != null)
		{
			if (node is Node3D node3D && node.Name == "Player")
				return node3D;

			node = node.GetParent();
		}

		return GetParent<Node3D>();
	}

	public override void _Process(double delta)
	{
		var profileMode = _modeDetector.GetMode();
		var controllerProfileMode = profileMode == PlayerInputMode.Controller;
		var leftTrackerActive = IsHandTrackerActive(_leftTrackedHand);
		var rightTrackerActive = IsHandTrackerActive(_rightTrackedHand);
		var anyHandTrackerActive = leftTrackerActive || rightTrackerActive;
		var anyControllerTrackerActive = IsControllerTrackerActive(_leftController) || IsControllerTrackerActive(_rightController);
		UpdateActiveVisualMode(controllerProfileMode, anyHandTrackerActive, anyControllerTrackerActive);
		var controllerMode = _activeVisualMode == PlayerInputMode.Controller;
		_leftHandTrackingActive = !controllerMode && leftTrackerActive;
		_rightHandTrackingActive = !controllerMode && rightTrackerActive;
		_gestures.SetRightPointerNativeActionEnabled(!_rightHandTrackingActive);
		_visuals.Apply(
			controllerMode,
			_leftHandTrackingActive,
			_rightHandTrackingActive,
			_leftMovement.IsLocked,
			IsPlayerMovementEnabled());

		if (controllerMode || (!_leftHandTrackingActive && !_rightHandTrackingActive))
		{
			_gestures.Reset();
			return;
		}

		_gestures.Process(_leftHandTrackingActive, _rightHandTrackingActive);
	}

	public override void _PhysicsProcess(double delta)
	{
		_gestures.ProcessMovement(_leftHandTrackingActive, IsPlayerMovementEnabled(), (float)delta);
	}

	private bool IsPlayerMovementEnabled()
	{
		return _playerBody.ProcessMode != ProcessModeEnum.Disabled;
	}

	private static bool IsHandTrackerActive(Node3D trackedHand)
	{
		return ((XRNode3D)trackedHand).GetIsActive();
	}

	private static bool IsControllerTrackerActive(XRController3D controller)
	{
		return controller.GetIsActive();
	}

	private void UpdateActiveVisualMode(bool controllerProfileMode, bool anyHandTrackerActive, bool anyControllerTrackerActive)
	{
		var controllerInputActive = controllerProfileMode && IsControllerInputActive();

		if (controllerInputActive)
		{
			_activeVisualMode = PlayerInputMode.Controller;
			return;
		}

		if (_activeVisualMode == PlayerInputMode.Controller)
		{
			if (!anyControllerTrackerActive && anyHandTrackerActive)
				_activeVisualMode = PlayerInputMode.Hand;

			return;
		}

		if (_activeVisualMode == PlayerInputMode.Hand)
		{
			if (!anyHandTrackerActive && anyControllerTrackerActive && controllerProfileMode)
				_activeVisualMode = PlayerInputMode.Controller;

			return;
		}

		_activeVisualMode = controllerProfileMode && anyControllerTrackerActive && !anyHandTrackerActive
			? PlayerInputMode.Controller
			: PlayerInputMode.Hand;
	}

	private bool IsControllerInputActive()
	{
		return IsControllerInputActive(_leftController) || IsControllerInputActive(_rightController);
	}

	private bool IsControllerInputActive(XRController3D controller)
	{
		var movement = controller.GetVector2(MovementAction);
		return movement.Length() > ControllerInputActiveThreshold ||
			controller.IsButtonPressed("primary_click") ||
			controller.IsButtonPressed("secondary_click") ||
			controller.IsButtonPressed("ax_button") ||
			controller.IsButtonPressed("by_button") ||
			controller.IsButtonPressed("menu_button");
	}
}