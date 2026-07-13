using Godot;
using Logger;

namespace BCSVRMuseum.Menu_Scripts;

public partial class SettingsPanel : Node
{
	private readonly EventLogger _logger = new(nameof(SettingsPanel));

	private Button _closeButton;
	private PlatformSwitcher _platformSwitcher;
	private Button _startButton;

	public override void _Ready()
	{
		var root = GetParent();

		_closeButton = (Button)root.FindChild("Close", true, false);
		_startButton = (Button)root.FindChild("Start", true, false);
		_platformSwitcher = (PlatformSwitcher)GetTree().Root.FindChild("PlatformSwitcher", true, false);

		_closeButton.Pressed += OnClosePressed;
		_startButton.Pressed += OnStartPressed;
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
