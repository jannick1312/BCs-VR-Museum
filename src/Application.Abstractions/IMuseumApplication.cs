using Models;

namespace Application.Abstractions;

public interface IMuseumApplication
{
	Task<DisplayMediaResult> SearchAsync(string text, int limit);
	Task<DisplayMediaResult> SearchAsync(IReadOnlyList<double> vector, int limit);
	Task<bool> IsReachableAsync();
}