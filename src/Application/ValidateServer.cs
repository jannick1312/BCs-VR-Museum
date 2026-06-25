namespace Application;

public class ValidateServer(IServerHealthService serverHealthService)
{
    public Task<bool> ExecuteAsync()
    {
        return serverHealthService.IsReachableAsync();
    }
}