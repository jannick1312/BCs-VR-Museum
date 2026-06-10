using Application;
using Godot;
using Infrastructure.Media;
using Infrastructure.Vitrivr;

namespace BCSVRMuseum;

public partial class SearchUseCaseFactory : Node
{
    private SearchSettingsStore _searchSettingsStore;

    private SearchMedia _cachedUseCase;
    private string _cachedIp = "";
    private string _cachedMediaFolderPath = "";

    public override void _Ready()
    {
        _searchSettingsStore = GetTree().Root.FindChild("SearchSettingsStore", true, false) as SearchSettingsStore;
    }

    public SearchMedia GetSearchAndLoadMedia()
    {
        if (_cachedUseCase != null &&  _cachedIp == _searchSettingsStore.CurrentIp && _cachedMediaFolderPath == _searchSettingsStore.MediaFolderPath)
        {
            return _cachedUseCase;
        }

        var vitrivrSettings = new VitrivrSettings(_searchSettingsStore.CurrentIp, _searchSettingsStore.MediaFolderPath);

        ISearchService searchService = new VitrivrSearchService(vitrivrSettings);

        IMediaLoader mediaLoader = new MediaLoader();

        _cachedUseCase = new SearchMedia(searchService, mediaLoader);

        _cachedIp = _searchSettingsStore.CurrentIp;
        _cachedMediaFolderPath = _searchSettingsStore.MediaFolderPath;

        return _cachedUseCase;
    }
}