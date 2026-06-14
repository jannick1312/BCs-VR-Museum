using Application;
using Godot;
using Infrastructure.Logging;
using Infrastructure.Media;
using Infrastructure.Vitrivr;

namespace BCSVRMuseum;

public partial class SearchUseCaseFactory : Node
{
    private readonly EventLogger _logger = new(nameof(SearchUseCaseFactory));
    private SearchSettingsStore _searchSettingsStore;

    public override void _Ready()
    {
        _searchSettingsStore = GetTree().Root.FindChild("SearchSettingsStore", true, false) as SearchSettingsStore;
    }

    public SearchMedia GetSearchAndLoadMedia()
    {
        var vitrivrSettings = new VitrivrSettings(_searchSettingsStore.CurrentIp, _searchSettingsStore.MediaFolderPath);

        _logger.Info($"Creating search use case. CurrentIp={_searchSettingsStore.CurrentIp}, MediaFolderPath='{_searchSettingsStore.MediaFolderPath}'");

        ISearchService searchService = new VitrivrSearchService(vitrivrSettings);

        IMediaLoader mediaLoader = new MediaLoader();

        return new SearchMedia(searchService, mediaLoader);
    }
}