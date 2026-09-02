using Godot;

namespace BCSVRMuseum.Player;

/// <summary>
/// Handles tracked-hand gestures, fallback hand poses, and hand actions.
/// </summary>
/// <param name="player">The player node.</param>
/// <param name="leftMovement">The virtual joystick controlled by the left hand.</param>
/// <param name="input">The hand action settings.</param>
public sealed class HandGestureInput(Node3D player, HandJoystickMovement leftMovement, PlayerHandInput input)
{
	private readonly XRController3D _leftController = FindController(player, "LeftController");
	private readonly Node _leftFallbackHand = player.FindChild("LeftFallbackHand", true, false);
	private readonly Node _leftPickup = FindController(player, "LeftController").FindChild("FunctionPickup", true, false);
	private readonly PlatformSwitcher _platformSwitcher = (PlatformSwitcher)player.GetTree().Root.FindChild("PlatformSwitcher", true, false);
	private readonly XRController3D _rightController = FindController(player, "RightController");
	private readonly Node _rightFallbackHand = player.FindChild("RightFallbackHand", true, false);
	private readonly Node _rightPickup = FindController(player, "RightController").FindChild("FunctionPickup", true, false);
	private readonly Node _rightPointer = FindController(player, "RightController").FindChild("FunctionPointer", true, false);
	private HandGesture _leftGesture;
	private bool _leftPinchMoved;
	private double _leftPinchStartedAt = -1.0;
	private HandGesture _rightGesture;
	private bool _rightPointerNativeActionDisabled;
	private bool _rightPointerPressedByHand;

	/// <summary>
	/// Updates fallback hand poses, detects gestures, and controls pickup.
	/// </summary>
	/// <param name="leftHandActive">If the left hand is actively tracked.</param>
	/// <param name="rightHandActive">If the right hand is actively tracked.</param>
	public void Process(bool leftHandActive, bool rightHandActive)
	{
		UpdateFallbackPose(_leftFallbackHand, _leftController, leftHandActive);
		UpdateFallbackPose(_rightFallbackHand, _rightController, rightHandActive);

		if (leftHandActive)
			_leftGesture = UpdateGesture(_leftController, _leftController.GetFloat(input.PinchAction), _leftGesture, HandSide.Left);
		else
			ResetLeftGesture();

		if (rightHandActive)
			_rightGesture = UpdateGesture(_rightController, _rightController.GetFloat(input.PinchAction), _rightGesture, HandSide.Right);
		else
			ResetRightGesture();

		SetPickupEnabled(_leftPickup, !leftHandActive || _leftGesture != HandGesture.Pinch);
		SetPickupEnabled(_rightPickup, !rightHandActive || _rightGesture != HandGesture.Pinch);
	}

	/// <summary>
	/// Starts virtual joystick movement after holding a left-hand pinch.
	/// </summary>
	/// <param name="leftHandActive">If the left hand is actively tracked.</param>
	/// <param name="playerMovementEnabled">If the player can move.</param>
	/// <param name="delta">The physics frame time in seconds.</param>
	public void ProcessMovement(bool leftHandActive, bool playerMovementEnabled, float delta)
	{
		if (!leftHandActive || _leftGesture != HandGesture.Pinch || !playerMovementEnabled)
		{
			leftMovement.ForceStop();
			return;
		}

		var heldLongEnough = _leftPinchStartedAt >= 0.0 && Time.GetTicksMsec() / 1000.0 - _leftPinchStartedAt >= input.LeftPinchMoveDelaySeconds;

		if (heldLongEnough)
		{
			_leftPinchMoved = true;
			leftMovement.ProcessMovement(delta);
		}
		else
		{
			leftMovement.ForceStop();
		}
	}

	/// <summary>
	/// Resets stored gestures, pickup, fallback hand poses, and hand movement.
	/// </summary>
	public void Reset()
	{
		ResetLeftGesture();
		ResetRightGesture();
		SetPickupEnabled(_leftPickup, true);
		SetPickupEnabled(_rightPickup, true);
		_leftFallbackHand.Call("force_grip_trigger", 0.0f, 0.0f);
		_rightFallbackHand.Call("force_grip_trigger", 0.0f, 0.0f);
		leftMovement.ForceStop();
	}

	/// <summary>
	/// Turns controller trigger clicks on or off for the right pointer.
	/// </summary>
	/// <param name="enabled">If the controller trigger should control pointer clicks.</param>
	public void SetRightPointerNativeActionEnabled(bool enabled)
	{
		if (enabled)
		{
			if (!_rightPointerNativeActionDisabled)
				return;

			_rightPointer.Set("active_button_action", "trigger_click");
			_rightPointerNativeActionDisabled = false;
			return;
		}

		if (_rightPointerNativeActionDisabled)
			return;

		_rightPointer.Set("active_button_action", "__hand_pointer_click_disabled");
		_rightPointerNativeActionDisabled = true;
	}

	/// <summary>
	/// Updates one hand's gesture using the pinch and grip limits.
	/// </summary>
	/// <param name="controller">The controller providing hand action values.</param>
	/// <param name="pinchValue">The current pinch action value.</param>
	/// <param name="current">The previous gesture state.</param>
	/// <param name="hand">The hand being evaluated.</param>
	/// <returns>The updated gesture state.</returns>
	private HandGesture UpdateGesture(XRController3D controller, float pinchValue, HandGesture current, HandSide hand)
	{
		var gripValue = controller.GetFloat(input.GripAction);

		if (current == HandGesture.Pinch)
		{
			if (pinchValue >= input.PinchReleaseThreshold)
				return HandGesture.Pinch;

			if (hand == HandSide.Left)
				FinishLeftPinch();
			else
				ReleaseRightPointerIfNeeded();

			return HandGesture.None;
		}

		if (current == HandGesture.Grab)
			return gripValue < input.GrabReleaseThreshold ? HandGesture.None : HandGesture.Grab;

		if (pinchValue > input.PinchThreshold)
		{
			if (hand == HandSide.Left)
				StartLeftPinch();
			else
				PressRightPointerIfNeeded();

			return HandGesture.Pinch;
		}

		return gripValue >= input.GrabThreshold ? HandGesture.Grab : HandGesture.None;
	}

	/// <summary>
	/// Clears the left-hand gesture and pinch timing state.
	/// </summary>
	private void ResetLeftGesture()
	{
		_leftGesture = HandGesture.None;
		_leftPinchStartedAt = -1.0;
		_leftPinchMoved = false;
	}

	/// <summary>
	/// Clears the right-hand gesture and releases pointer press.
	/// </summary>
	private void ResetRightGesture()
	{
		_rightGesture = HandGesture.None;
		ReleaseRightPointerIfNeeded();
	}

	/// <summary>
	/// Starts timing a left-hand pinch for a tap or movement.
	/// </summary>
	private void StartLeftPinch()
	{
		_leftPinchStartedAt = Time.GetTicksMsec() / 1000.0;
		_leftPinchMoved = false;
	}

	/// <summary>
	/// Switches the world after a short pinch.
	/// </summary>
	private void FinishLeftPinch()
	{
		var duration = _leftPinchStartedAt >= 0.0 ? Time.GetTicksMsec() / 1000.0 - _leftPinchStartedAt : double.PositiveInfinity;

		if (!_leftPinchMoved && duration < input.LeftPinchMoveDelaySeconds)
			_platformSwitcher.ToggleWorld();

		ResetLeftGesture();
	}

	/// <summary>
	/// Presses the right-hand pointer when a pinch starts.
	/// </summary>
	private void PressRightPointerIfNeeded()
	{
		if (_rightPointerPressedByHand)
			return;

		_rightPointerPressedByHand = true;
		_rightPointer.Call("_button_pressed");
	}

	/// <summary>
	/// Releases a pointer press initiated by the right hand.
	/// </summary>
	private void ReleaseRightPointerIfNeeded()
	{
		if (!_rightPointerPressedByHand)
			return;

		_rightPointerPressedByHand = false;
		_rightPointer.Call("_button_released");
	}

	/// <summary>
	/// Enables or disables a hand's pickup interaction.
	/// </summary>
	/// <param name="pickup">The pickup component to update.</param>
	/// <param name="enabled">If pickup should be enabled.</param>
	private static void SetPickupEnabled(Node pickup, bool enabled)
	{
		pickup.Set("enabled", enabled);
	}

	/// <summary>
	/// Sets the fallback hand pose from grip and pinch values.
	/// </summary>
	/// <param name="fallbackHand">The fallback hand visual.</param>
	/// <param name="controller">The controller providing hand action values.</param>
	/// <param name="handActive">If hand tracking is active.</param>
	private void UpdateFallbackPose(Node fallbackHand, XRController3D controller, bool handActive)
	{
		var grip = handActive ? controller.GetFloat(input.GripAction) : 0.0f;
		var pinch = handActive ? controller.GetFloat(input.PinchAction) : 0.0f;
		fallbackHand.Call("force_grip_trigger", grip, pinch);
	}

	/// <summary>
	/// Finds a controller connected to the player.
	/// </summary>
	/// <param name="player">The player node.</param>
	/// <param name="name">The controller node name.</param>
	/// <returns>The matching controller.</returns>
	private static XRController3D FindController(Node player, string name)
	{
		return (XRController3D)player.FindChild(name, true, false);
	}
}



// Codex helped implement the fallback hand poses used when hand tracking is active but tracked hand meshes are unavailable.
