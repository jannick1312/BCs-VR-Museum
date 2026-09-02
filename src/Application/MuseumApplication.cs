using Application.Abstractions;
using Models;

namespace Application;

/// <summary>
/// Connects the museum to media search and server services.
/// </summary>
/// <param name="searchEngine">The search engine used to find media.</param>
/// <param name="mediaLoader">The loader used for media files.</param>
/// <param name="serverHealthService">The service used to check if the server is online.</param>
public sealed class MuseumApplication(ISearchEngine searchEngine, IMediaLoader mediaLoader, IServerHealthService serverHealthService) : IMuseumApplication
{
	private readonly SearchMedia _searchMedia = new(searchEngine, mediaLoader);
	private readonly ValidateServer _validateServer = new(serverHealthService);

	/// <summary>
	/// Runs the museum text search.
	/// </summary>
	/// <param name="text">The text to search for.</param>
	/// <param name="limit">The maximum number of results to return.</param>
	/// <param name="mediaMode">The types of media to include.</param>
	/// <param name="maxMedia2D">The maximum number of images and videos to load.</param>
	/// <param name="maxObjects3D">The maximum number of 3D models to load.</param>
	/// <returns>A task containing the loaded media result.</returns>
	public Task<DisplayMediaResult> SearchAsync(string text, int limit, MediaMode mediaMode, int maxMedia2D, int maxObjects3D)
	{
		return _searchMedia.ExecuteAsync(text, limit, mediaMode, maxMedia2D, maxObjects3D);
	}

	/// <summary>
	/// Runs the museum similarity search.
	/// </summary>
	/// <param name="vector">The feature vector used for the search.</param>
	/// <param name="limit">The maximum number of results to return.</param>
	/// <param name="mediaMode">The types of media to include.</param>
	/// <param name="maxMedia2D">The maximum number of images and videos to load.</param>
	/// <param name="maxObjects3D">The maximum number of 3D models to load.</param>
	/// <returns>A task containing the loaded media result.</returns>
	public Task<DisplayMediaResult> SearchAsync(IReadOnlyList<double> vector, int limit, MediaMode mediaMode, int maxMedia2D, int maxObjects3D)
	{
		return _searchMedia.ExecuteAsync(vector, limit, mediaMode, maxMedia2D, maxObjects3D);
	}

	/// <summary>
	/// Removes downloaded files from the previous search.
	/// </summary>
	public void CompleteMediaPlacement()
	{
		mediaLoader.ReleasePreviousBatch();
	}

	/// <summary>
	/// Runs the museum server check.
	/// </summary>
	/// <param name="cancellation">A token that stops the check.</param>
	/// <returns>A task containing <see langword="true"/> if the server is reachable and <see langword="false"/> otherwise.</returns>
	public Task<bool> IsReachableAsync(CancellationToken cancellation)
	{
		return _validateServer.ExecuteAsync(cancellation);
	}
}
