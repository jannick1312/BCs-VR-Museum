using System;
using Godot;
using Logger;

namespace BCSVRMuseum.Player;

public sealed class PlayerInputModeDetector(Node player)
{
	private readonly GameSettingsStore _gameSettingsStore = (GameSettingsStore)player.GetTree().Root.FindChild("GameSettingsStore", true, false);
	private readonly EventLogger _logger = new(nameof(PlayerInputModeDetector));
	private readonly XRController3D _rightController = (XRController3D)player.FindChild("RightController", true, false);
	private PlayerInputMode? _lastLoggedMode;

	public PlayerInputMode GetMode()
	{
		var mode = !_gameSettingsStore.HandTrackingEnabled || IsControllerProfile(GetTrackerProfile(_rightController)) ? PlayerInputMode.Controller : PlayerInputMode.Hand;

		if (mode == _lastLoggedMode)
			return mode;
		_lastLoggedMode = mode;
		_logger.Info($"Player input mode: {mode}");
		return mode;
	}

	private static string GetTrackerProfile(XRController3D controller)
	{
		var tracker = XRServer.GetTracker(controller.Tracker);
		return tracker.Get("profile").AsString();
	}

	private static bool IsControllerProfile(string profile)
	{
		return !profile.Contains("hand_interaction", StringComparison.OrdinalIgnoreCase);
	}
}
