using Godot;

namespace BCSVRMuseum.Player;

/// <summary>
/// Updates controller and hand visuals and controller movement.
/// </summary>
/// <param name="player">The player node.</param>
public sealed class PlayerInputVisuals(Node3D player)
{
	private readonly Node3D _leftControllerModel = FindControllerNode(player, "LeftController", "LeftControllerModel");
	private readonly Node3D _leftFallbackHand = (Node3D)player.FindChild("LeftFallbackHand", true, false);
	private readonly Node3D _leftHandMesh = (Node3D)player.FindChild("LeftHandTrackingMesh", true, false);
	private readonly Node3D _leftTrackedHand = (Node3D)player.FindChild("LeftTrackedHand", true, false);
	private readonly Node _movementDirect = player.FindChild("MovementDirect", true, false);
	private readonly Node _movementTurn = player.FindChild("MovementTurn", true, false);
	private readonly Node3D _rightControllerModel = FindControllerNode(player, "RightController", "RightControllerModel");
	private readonly Node3D _rightFallbackHand = (Node3D)player.FindChild("RightFallbackHand", true, false);
	private readonly Node3D _rightHandMesh = (Node3D)player.FindChild("RightHandTrackingMesh", true, false);
	private readonly Node3D _rightTrackedHand = (Node3D)player.FindChild("RightTrackedHand", true, false);

	/// <summary>
	/// Shows the correct controller or hand models and sets controller movement.
	/// </summary>
	/// <param name="controllerMode">If controller input is active.</param>
	/// <param name="leftHandActive">If the left hand tracker is active.</param>
	/// <param name="rightHandActive">If the right hand tracker is active.</param>
	/// <param name="leftFallbackRequired">If the left fallback hand must be shown.</param>
	/// <param name="rightFallbackRequired">If the right fallback hand must be shown.</param>
	/// <param name="joystickLocked">If the left hand is controlling the virtual joystick.</param>
	/// <param name="playerMovementEnabled">If the player can move.</param>
	public void Apply(bool controllerMode, bool leftHandActive, bool rightHandActive, bool leftFallbackRequired, bool rightFallbackRequired, bool joystickLocked, bool playerMovementEnabled)
	{
		_leftControllerModel.Visible = controllerMode;
		_rightControllerModel.Visible = controllerMode;
		_leftTrackedHand.Visible = leftHandActive;
		_rightTrackedHand.Visible = rightHandActive;
		_leftHandMesh.Visible = leftHandActive && !leftFallbackRequired && !joystickLocked;
		_rightHandMesh.Visible = rightHandActive && !rightFallbackRequired;
		_leftFallbackHand.Visible = leftHandActive && leftFallbackRequired && !joystickLocked;
		_rightFallbackHand.Visible = rightHandActive && rightFallbackRequired;

		SetControllerMovementEnabled(controllerMode && playerMovementEnabled);
	}

	/// <summary>
	/// Turns controller movement on or off.
	/// </summary>
	/// <param name="enabled">If controller movement should be on.</param>
	private void SetControllerMovementEnabled(bool enabled)
	{
		SetProcessEnabled(_movementDirect, enabled);
		SetProcessEnabled(_movementTurn, enabled);
	}

	/// <summary>
	/// Turns a movement node and its functions on or off.
	/// </summary>
	/// <param name="node">The movement node to update.</param>
	/// <param name="enabled">If the node should be on.</param>
	private static void SetProcessEnabled(Node node, bool enabled)
	{
		node.Set("enabled", enabled);
		node.ProcessMode = enabled ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
	}

	/// <summary>
	/// Finds a model below a controller.
	/// </summary>
	/// <param name="player">The player node.</param>
	/// <param name="controllerName">The controller node name.</param>
	/// <param name="nodeName">The model node name.</param>
	/// <returns>The requested controller model.</returns>
	private static Node3D FindControllerNode(Node player, string controllerName, string nodeName)
	{
		return (Node3D)player.FindChild(controllerName, true, false).FindChild(nodeName, false, false);
	}
}



// Codex helped implement the fallback hand display used when hand tracking is active but tracked hand meshes are unavailable.
