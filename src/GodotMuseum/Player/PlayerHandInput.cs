using Godot;

namespace BCSVRMuseum.Player;

/// <summary>
/// Manages controller and tracked-hand input.
/// </summary>
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

	/// <summary>
	/// Finds player input nodes and creates hand input helpers.
	/// </summary>
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

	/// <summary>
	/// Finds the player node in the scene tree.
	/// </summary>
	/// <returns>The player node.</returns>
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

	/// <summary>
	/// Updates the input mode, hand visuals, and tracked-hand gestures.
	/// </summary>
	/// <param name="delta">The frame time in seconds.</param>
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

	/// <summary>
	/// Updates hand movement during physics frames.
	/// </summary>
	/// <param name="delta">The physics frame time in seconds.</param>
	public override void _PhysicsProcess(double delta)
	{
		_gestures.ProcessMovement(_leftHandTrackingActive, IsPlayerMovementEnabled(), (float)delta);
	}

	/// <summary>
	/// Checks if player movement is turned on.
	/// </summary>
	/// <returns><see langword="true"/> when player movement is enabled and <see langword="false"/> otherwise.</returns>
	private bool IsPlayerMovementEnabled()
	{
		return _playerBody.ProcessMode != ProcessModeEnum.Disabled;
	}

	/// <summary>
	/// Checks if a hand is being tracked.
	/// </summary>
	/// <param name="trackedHand">The tracked-hand node to inspect.</param>
	/// <returns><see langword="true"/> if tracking is active and <see langword="false"/> otherwise.</returns>
	private static bool IsHandTrackerActive(Node3D trackedHand)
	{
		return ((XRNode3D)trackedHand).GetIsActive();
	}
}
