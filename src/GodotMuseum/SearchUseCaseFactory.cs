using Application.Abstractions;
using Application.Factory;
using Godot;
using Logger;

namespace BCSVRMuseum;

/// <summary>
/// Creates museum applications from the settings.
/// </summary>
public partial class SearchUseCaseFactory : Node
{
	private readonly EventLogger _logger = new(nameof(SearchUseCaseFactory));

	private SearchSettingsStore _searchSettingsStore;

	/// <summary>
	/// Finds the settings store.
	/// </summary>
	public override void _Ready()
	{
		_searchSettingsStore = (SearchSettingsStore)GetTree().Root.FindChild("SearchSettingsStore", true, false);
	}

	/// <summary>
	/// Uses the current settings to create a museum application.
	/// </summary>
	/// <returns>The new museum application.</returns>
	public IMuseumApplication GetMuseumApplication()
	{
		_logger.Info($"Museum application created. CurrentIp='{_searchSettingsStore.CurrentIp}', MediaFolderPath='{_searchSettingsStore.CurrentMediaFolderPath}'.");
		return MuseumApplicationFactory.CreateVitrivrApplication(_searchSettingsStore.CurrentIp, _searchSettingsStore.CurrentMediaFolderPath, ProjectSettings.GlobalizePath("user://"));
	}
}
