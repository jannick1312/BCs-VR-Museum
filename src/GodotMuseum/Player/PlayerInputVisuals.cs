using Godot;

namespace BCSVRMuseum.Player;

public sealed class PlayerInputVisuals(Node3D player)
{
	private readonly Node _movementDirect = player.FindChild("MovementDirect", true, false);
	private readonly Node _movementTurn = player.FindChild("MovementTurn", true, false);
	private readonly Node3D _leftControllerModel = FindControllerNode(player, "LeftController", "LeftControllerModel");
	private readonly Node3D _rightControllerModel = FindControllerNode(player, "RightController", "RightControllerModel");
	private readonly Node3D _leftControllerHand = FindControllerNode(player, "LeftController", "LeftHand");
	private readonly Node3D _rightControllerHand = FindControllerNode(player, "RightController", "RightHand");
	private readonly Node3D _leftTrackedHand = (Node3D)player.FindChild("LeftTrackedHand", true, false);
	private readonly Node3D _rightTrackedHand = (Node3D)player.FindChild("RightTrackedHand", true, false);
	private readonly Node3D _leftHandMesh = (Node3D)player.FindChild("LeftHandTrackingMesh", true, false);
	private readonly Node3D _rightHandMesh = (Node3D)player.FindChild("RightHandTrackingMesh", true, false);

	public void Apply(bool controllerMode, bool leftHandActive, bool rightHandActive, bool joystickLocked, bool playerMovementEnabled)
	{
		_leftControllerModel.Visible = controllerMode;
		_rightControllerModel.Visible = controllerMode;
		_leftControllerHand.Visible = false;
		_rightControllerHand.Visible = false;
		_leftTrackedHand.Visible = leftHandActive;
		_rightTrackedHand.Visible = rightHandActive;
		_leftHandMesh.Visible = leftHandActive && !joystickLocked;
		_rightHandMesh.Visible = rightHandActive;

		SetControllerMovementEnabled(controllerMode && playerMovementEnabled);
	}

	private void SetControllerMovementEnabled(bool enabled)
	{
		SetProcessEnabled(_movementDirect, enabled);
		SetProcessEnabled(_movementTurn, enabled);
	}

	private static void SetProcessEnabled(Node node, bool enabled)
	{
		node.Set("enabled", enabled);
		node.ProcessMode = enabled ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
	}

	private static Node3D FindControllerNode(Node player, string controllerName, string nodeName)
	{
		return (Node3D)player.FindChild(controllerName, true, false).FindChild(nodeName, false, false);
	}
}