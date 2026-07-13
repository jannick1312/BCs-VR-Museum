using Godot;

namespace BCSVRMuseum.Player;

public partial class PlayerHandInput : Node
{
	private HandGestureInput _gestures;
	private bool _leftFallbackRequired;
	private Node _leftHandMesh;
	private bool _leftHandTrackingActive;
	private HandJoystickMovement _leftMovement;
	private Node3D _leftTrackedHand;
	private PlayerInputModeDetector _modeDetector;
	private Node3D _player;
	private Node _playerBody;
	private bool _rightFallbackRequired;
	private Node _rightHandMesh;
	private bool _rightHandTrackingActive;
	private Node3D _rightTrackedHand;
	private PlayerInputVisuals _visuals;

	[Export] public StringName PinchAction { get; set; }
	[Export] public StringName GripAction { get; set; }
	[Export] public float PinchThreshold { get; set; }
	[Export] public float PinchReleaseThreshold { get; set; }
	[Export] public float GrabThreshold { get; set; }
	[Export] public float GrabReleaseThreshold { get; set; }
	[Export] public float LeftPinchMoveDelaySeconds { get; set; }

	public override void _Ready()
	{
		_player = FindPlayer();
		_playerBody = _player.FindChild("PlayerBody", true, false);
		_leftTrackedHand = (Node3D)_player.FindChild("LeftTrackedHand", true, false);
		_rightTrackedHand = (Node3D)_player.FindChild("RightTrackedHand", true, false);
		_leftHandMesh = _player.FindChild("LeftHandTrackingMesh", true, false);
		_rightHandMesh = _player.FindChild("RightHandTrackingMesh", true, false);
		_leftHandMesh.Connect("openxr_fb_hand_tracking_mesh_ready", Callable.From(() => _leftFallbackRequired = false));
		_leftHandMesh.Connect("openxr_fb_hand_tracking_mesh_unavailable", Callable.From(() => _leftFallbackRequired = true));
		_rightHandMesh.Connect("openxr_fb_hand_tracking_mesh_ready", Callable.From(() => _rightFallbackRequired = false));
		_rightHandMesh.Connect("openxr_fb_hand_tracking_mesh_unavailable", Callable.From(() => _rightFallbackRequired = true));
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
		var controllerMode = _modeDetector.GetMode() == PlayerInputMode.Controller;
		_leftHandTrackingActive = !controllerMode && IsHandTrackerActive(_leftTrackedHand);
		_rightHandTrackingActive = !controllerMode && IsHandTrackerActive(_rightTrackedHand);
		_gestures.SetRightPointerNativeActionEnabled(!_rightHandTrackingActive);
		_visuals.Apply(controllerMode, _leftHandTrackingActive, _rightHandTrackingActive, _leftFallbackRequired, _rightFallbackRequired, _leftMovement.IsLocked, IsPlayerMovementEnabled());

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
}
