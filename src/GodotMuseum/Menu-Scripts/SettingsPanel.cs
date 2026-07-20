using Godot;
using Logger;

namespace BCSVRMuseum.Menu_Scripts;

public partial class SettingsPanel : Node
{
	private readonly EventLogger _logger = new(nameof(SettingsPanel));

	private Button _closeButton;
	private MuseumEntryState _entryState;
	private PlatformSwitcher _platformSwitcher;
	private Button _startButton;

	public override void _Ready()
	{
		var root = GetParent();

		_closeButton = (Button)root.FindChild("Close", true, false);
		_startButton = (Button)root.FindChild("Start", true, false);
		_platformSwitcher = (PlatformSwitcher)GetTree().Root.FindChild("PlatformSwitcher", true, false);
		var searchSettingsStore = (SearchSettingsStore)GetTree().Root.FindChild("SearchSettingsStore", true, false);
		_entryState = searchSettingsStore.EntryState;

		_closeButton.Pressed += OnClosePressed;
		_startButton.Pressed += OnStartPressed;
		_entryState.Changed += UpdateStartButton;
		UpdateStartButton();
	}

	public override void _ExitTree()
	{
		_entryState?.Changed -= UpdateStartButton;
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

	private void UpdateStartButton()
	{
		_startButton.Disabled = !_entryState.CanEnterMuseum;
	}
}
