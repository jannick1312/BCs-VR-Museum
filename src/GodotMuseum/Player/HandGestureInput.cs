using Godot;

namespace BCSVRMuseum.Player;

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

	private void ResetLeftGesture()
	{
		_leftGesture = HandGesture.None;
		_leftPinchStartedAt = -1.0;
		_leftPinchMoved = false;
	}

	private void ResetRightGesture()
	{
		_rightGesture = HandGesture.None;
		ReleaseRightPointerIfNeeded();
	}

	private void StartLeftPinch()
	{
		_leftPinchStartedAt = Time.GetTicksMsec() / 1000.0;
		_leftPinchMoved = false;
	}

	private void FinishLeftPinch()
	{
		var duration = _leftPinchStartedAt >= 0.0 ? Time.GetTicksMsec() / 1000.0 - _leftPinchStartedAt : double.PositiveInfinity;

		if (!_leftPinchMoved && duration < input.LeftPinchMoveDelaySeconds)
			_platformSwitcher.ToggleWorld();

		ResetLeftGesture();
	}

	private void PressRightPointerIfNeeded()
	{
		if (_rightPointerPressedByHand)
			return;

		_rightPointerPressedByHand = true;
		_rightPointer.Call("_button_pressed");
	}

	private void ReleaseRightPointerIfNeeded()
	{
		if (!_rightPointerPressedByHand)
			return;

		_rightPointerPressedByHand = false;
		_rightPointer.Call("_button_released");
	}

	private static void SetPickupEnabled(Node pickup, bool enabled)
	{
		pickup.Set("enabled", enabled);
	}

	private void UpdateFallbackPose(Node fallbackHand, XRController3D controller, bool handActive)
	{
		var grip = handActive ? controller.GetFloat(input.GripAction) : 0.0f;
		var pinch = handActive ? controller.GetFloat(input.PinchAction) : 0.0f;
		fallbackHand.Call("force_grip_trigger", grip, pinch);
	}

	private static XRController3D FindController(Node player, string name)
	{
		return (XRController3D)player.FindChild(name, true, false);
	}
}
