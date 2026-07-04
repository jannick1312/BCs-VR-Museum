using Godot;
using Logger;

namespace BCSVRMuseum.Player;

public sealed class PlayerInputModeDetector(Node player)
{
	private readonly EventLogger _logger = new(nameof(PlayerInputModeDetector));
	private readonly XRController3D _leftController = (XRController3D)player.FindChild("LeftController", true, false);
	private readonly XRController3D _rightController = (XRController3D)player.FindChild("RightController", true, false);
	private string _lastLoggedMode = "";

	public PlayerInputMode GetMode()
	{
		var leftProfile = GetTrackerProfile(_leftController);
		var rightProfile = GetTrackerProfile(_rightController);
		var controllerMode = IsControllerProfile(leftProfile) || IsControllerProfile(rightProfile);

		var mode = controllerMode ? PlayerInputMode.Controller : PlayerInputMode.Hand;
		var modeLog = $"{mode} ({leftProfile} | {rightProfile})";
		if (modeLog == _lastLoggedMode) return mode;
		_lastLoggedMode = modeLog;
		_logger.Info($"Player input mode: {modeLog}");

		return mode;
	}

	private static string GetTrackerProfile(XRController3D controller)
	{
		var tracker = XRServer.GetTracker(controller.Tracker);
		return tracker.Get("profile").AsString();
	}

	private static bool IsControllerProfile(string profile)
	{
		return !profile.Contains("hand_interaction", System.StringComparison.InvariantCultureIgnoreCase);
	}
}