using Models;

namespace Application.Abstractions;

public interface IMuseumApplication
{
	Task<DisplayMediaResult> SearchAsync(string text, int limit, MediaMode mediaMode, int maxMedia2D, int maxObjects3D);
	Task<DisplayMediaResult> SearchAsync(IReadOnlyList<double> vector, int limit, MediaMode mediaMode, int maxMedia2D, int maxObjects3D);
	Task<bool> IsReachableAsync();
}
