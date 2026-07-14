using Application.Abstractions;
using Models;

namespace Application;

public sealed class MuseumApplication(ISearchEngine searchEngine, IMediaLoader mediaLoader, IServerHealthService serverHealthService) : IMuseumApplication
{
	private readonly SearchMedia _searchMedia = new(searchEngine, mediaLoader);
	private readonly ValidateServer _validateServer = new(serverHealthService);

	public Task<DisplayMediaResult> SearchAsync(string text, int limit, MediaMode mediaMode, int maxMedia2D, int maxObjects3D)
	{
		return _searchMedia.ExecuteAsync(text, limit, mediaMode, maxMedia2D, maxObjects3D);
	}

	public Task<DisplayMediaResult> SearchAsync(IReadOnlyList<double> vector, int limit, MediaMode mediaMode, int maxMedia2D, int maxObjects3D)
	{
		return _searchMedia.ExecuteAsync(vector, limit, mediaMode, maxMedia2D, maxObjects3D);
	}

	public void CompleteMediaPlacement()
	{
		mediaLoader.ReleasePreviousBatch();
	}

	public Task<bool> IsReachableAsync()
	{
		return _validateServer.ExecuteAsync();
	}
}
