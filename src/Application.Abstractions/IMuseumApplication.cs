using Models;

namespace Application.Abstractions;

/// <summary>
/// Provides media search and server functions for the museum.
/// </summary>
public interface IMuseumApplication
{
	/// <summary>
	/// Runs a text search for the museum.
	/// </summary>
	/// <param name="text">The text to search for.</param>
	/// <param name="limit">The maximum number of results to return.</param>
	/// <param name="mediaMode">The types of media to include.</param>
	/// <param name="maxMedia2D">The maximum number of images and videos to load.</param>
	/// <param name="maxObjects3D">The maximum number of 3D models to load.</param>
	/// <returns>A task containing the loaded media result.</returns>
	Task<DisplayMediaResult> SearchAsync(string text, int limit, MediaMode mediaMode, int maxMedia2D, int maxObjects3D);

	/// <summary>
	/// Runs a similarity search for the museum.
	/// </summary>
	/// <param name="vector">The feature vector used for the similarity search.</param>
	/// <param name="limit">The maximum number of results to return.</param>
	/// <param name="mediaMode">The types of media to include.</param>
	/// <param name="maxMedia2D">The maximum number of images and videos to load.</param>
	/// <param name="maxObjects3D">The maximum number of 3D models to load.</param>
	/// <returns>A task containing the loaded media result.</returns>
	Task<DisplayMediaResult> SearchAsync(IReadOnlyList<double> vector, int limit, MediaMode mediaMode, int maxMedia2D, int maxObjects3D);

	/// <summary>
	/// Removes downloaded files from the previous search.
	/// </summary>
	void CompleteMediaPlacement();

	/// <summary>
	/// Runs the current server check for the museum.
	/// </summary>
	/// <param name="cancellation">A token that cancels the health check.</param>
	/// <returns>A task containing <see langword="true"/> if the server is reachable and <see langword="false"/> otherwise.</returns>
	Task<bool> IsReachableAsync(CancellationToken cancellation);
}
