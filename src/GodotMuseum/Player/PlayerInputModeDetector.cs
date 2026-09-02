using System;
using Godot;
using Logger;

namespace BCSVRMuseum.Player;

/// <summary>
/// Selects controller or hand input.
/// </summary>
/// <param name="player">The player node.</param>
public sealed class PlayerInputModeDetector(Node player)
{
	private readonly GameSettingsStore _gameSettingsStore = (GameSettingsStore)player.GetTree().Root.FindChild("GameSettingsStore", true, false);
	private readonly EventLogger _logger = new(nameof(PlayerInputModeDetector));
	private readonly XRController3D _rightController = (XRController3D)player.FindChild("RightController", true, false);
	private PlayerInputMode? _lastLoggedMode;

	/// <summary>
	/// Gets the input mode for the device profile.
	/// </summary>
	/// <returns>The active player input mode.</returns>
	public PlayerInputMode GetMode()
	{
		var mode = !_gameSettingsStore.HandTrackingEnabled || IsControllerProfile(GetTrackerProfile(_rightController)) ? PlayerInputMode.Controller : PlayerInputMode.Hand;

		if (mode == _lastLoggedMode)
			return mode;
		_lastLoggedMode = mode;
		_logger.Info($"Player input mode: {mode}");
		return mode;
	}

	/// <summary>
	/// Gets the controller's tracker profile.
	/// </summary>
	/// <param name="controller">The controller to inspect.</param>
	/// <returns>The tracker profile name.</returns>
	private static string GetTrackerProfile(XRController3D controller)
	{
		var tracker = XRServer.GetTracker(controller.Tracker);
		return tracker.Get("profile").AsString();
	}

	/// <summary>
	/// Checks if a tracker profile belongs to a controller.
	/// </summary>
	/// <param name="profile">The tracker profile name.</param>
	/// <returns><see langword="true"/> for a controller profile and <see langword="false"/> otherwise.</returns>
	private static bool IsControllerProfile(string profile)
	{
		return !profile.Contains("hand_interaction", StringComparison.OrdinalIgnoreCase);
	}
}
