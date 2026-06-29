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
        _searchSettingsStore = GetTree().Root.FindChild("SearchSettingsStore", true, false) as SearchSettingsStore;
    }

    public IMuseumApplication GetMuseumApplication()
    {
        _logger.Info($"Creating museum application. CurrentIp={_searchSettingsStore.CurrentIp}, MediaFolderPath='{_searchSettingsStore.CurrentMediaFolderPath}'");

        return MuseumApplicationFactory.CreateVitrivrApplication(_searchSettingsStore.CurrentIp, _searchSettingsStore.CurrentMediaFolderPath);
    }
}