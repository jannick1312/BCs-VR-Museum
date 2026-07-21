namespace Application;

public class ValidateServer(IServerHealthService serverHealthService)
{
	public Task<bool> ExecuteAsync(CancellationToken cancellation)
	{
		return serverHealthService.IsReachableAsync(cancellation);
	}
}
