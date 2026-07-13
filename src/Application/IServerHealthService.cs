namespace Application;

public interface IServerHealthService
{
	Task<bool> IsReachableAsync();
}
