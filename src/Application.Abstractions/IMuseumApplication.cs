using Models;

namespace Application.Abstractions;

public interface IMuseumApplication
{
	Task<DisplayMediaResult> SearchAsync(string text, int limit);
	Task<bool> IsReachableAsync();
}