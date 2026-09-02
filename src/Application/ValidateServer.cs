namespace Application;

/// <summary>
/// Runs the server check used by the application.
/// </summary>
/// <param name="serverHealthService">The service used to check if the server is online.</param>
public class ValidateServer(IServerHealthService serverHealthService)
{
	/// <summary>
	/// Runs the server check.
	/// </summary>
	/// <param name="cancellation">A token that cancels the check.</param>
	/// <returns>A task containing <see langword="true"/> if the server is reachable and <see langword="false"/> otherwise.</returns>
	public Task<bool> ExecuteAsync(CancellationToken cancellation)
	{
		return serverHealthService.IsReachableAsync(cancellation);
	}
}
