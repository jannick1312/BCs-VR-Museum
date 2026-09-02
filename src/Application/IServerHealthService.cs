namespace Application;

/// <summary>
/// Provides a way to check if a media server can be reached.
/// </summary>
public interface IServerHealthService
{
	/// <summary>
	/// Checks if the media server can be reached.
	/// </summary>
	/// <param name="cancellation">A token that cancels the health check.</param>
	/// <returns>A task containing <see langword="true"/> if the server is reachable and <see langword="false"/> otherwise.</returns>
	Task<bool> IsReachableAsync(CancellationToken cancellation);
}
