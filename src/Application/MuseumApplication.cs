using Application.Abstractions;
using Models;

namespace Application;

public sealed class MuseumApplication(ISearchEngine searchEngine, IMediaLoader mediaLoader, IServerHealthService serverHealthService) : IMuseumApplication
{
    private readonly SearchMedia _searchMedia = new(searchEngine, mediaLoader);
    private readonly ValidateServer _validateServer = new(serverHealthService);

    public Task<DisplayMediaResult> SearchAsync(string text, int limit)
    {
        return _searchMedia.ExecuteAsync(text, limit);
    }

    public Task<bool> IsReachableAsync()
    {
        return _validateServer.ExecuteAsync();
    }
}