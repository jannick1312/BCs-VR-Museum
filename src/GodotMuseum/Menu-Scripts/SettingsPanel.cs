using Godot;
using Logger;

namespace BCSVRMuseum.Menu_Scripts;

/// <summary>
/// Controls the main settings panel's start and close actions.
/// </summary>
public partial class SettingsPanel : Node
{
	private readonly EventLogger _logger = new(nameof(SettingsPanel));

	private Button _closeButton;
	private MuseumEntryState _entryState;
	private PlatformSwitcher _platformSwitcher;
	private Button _startButton;

	/// <summary>
	/// Finds the panel controls and connects their events.
	/// </summary>
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

	/// <summary>
	/// Stops updating the start button when leaving the scene.
	/// </summary>
	public override void _ExitTree()
	{
		_entryState?.Changed -= UpdateStartButton;
	}

	/// <summary>
	/// Closes the application.
	/// </summary>
	private void OnClosePressed()
	{
		_logger.Info("Application quit requested from settings panel.");
		GetTree().Quit();
	}

	/// <summary>
	/// Requests a switch from the menu to the museum.
	/// </summary>
	private void OnStartPressed()
	{
		_platformSwitcher.SwitchToMuseum();
	}

	/// <summary>
	/// Enables the start button when all museum entry requirements are met.
	/// </summary>
	private void UpdateStartButton()
	{
		_startButton.Disabled = !_entryState.CanEnterMuseum;
	}
}
