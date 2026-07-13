using Application.Abstractions;
using Application.Factory;
using Godot;
using Logger;

namespace BCSVRMuseum;

public partial class SearchUseCaseFactory : Node
{
	private readonly EventLogger _logger = new(nameof(SearchUseCaseFactory));
	private SearchSettingsStore _searchSettingsStore;

	public override void _Ready()
	{
		_searchSettingsStore = (SearchSettingsStore)GetTree().Root.FindChild("SearchSettingsStore", true, false);
	}

	public IMuseumApplication GetMuseumApplication()
	{
		_logger.Info($"Museum application created. CurrentIp='{_searchSettingsStore.CurrentIp}', MediaFolderPath='{_searchSettingsStore.CurrentMediaFolderPath}'.");

		return MuseumApplicationFactory.CreateVitrivrApplication(_searchSettingsStore.CurrentIp, _searchSettingsStore.CurrentMediaFolderPath);
	}
}
