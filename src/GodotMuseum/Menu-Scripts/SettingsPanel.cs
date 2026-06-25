using Godot;
using Infrastructure.Logging;

namespace BCSVRMuseum.Menu_Scripts;

public partial class SettingsPanel : Node
{
	private readonly EventLogger _logger = new(nameof(SettingsPanel));

	private Button _closeButton;
	private Button _startButton;
	private PlatformSwitcher _platformSwitcher;

	public override void _Ready()
	{
		var root = GetParent();

		_closeButton = root.FindChild("Close", true, false) as Button;
		_startButton = root.FindChild("Start", true, false) as Button;
		_platformSwitcher = GetTree().Root.FindChild("PlatformSwitcher", true, false) as PlatformSwitcher;

		_closeButton?.Pressed += OnClosePressed;
		_startButton?.Pressed += OnStartPressed;
	}

	private void OnClosePressed()
	{
		_logger.Info("Application quit requested from settings panel.");
		GetTree().Quit();
	}

	private void OnStartPressed()
	{
		_platformSwitcher.SwitchToMuseum();
	}
}