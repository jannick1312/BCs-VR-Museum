using Application;
using Logger;

namespace Infrastructure.Vitrivr;

public class VitrivrServerHealthService(VitrivrSettings settings) : IServerHealthService
{
	private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(1) };
	private readonly EventLogger _logger = new(nameof(VitrivrServerHealthService));

	public async Task<bool> IsReachableAsync(CancellationToken cancellation)
	{
		for (var attempt = 0; attempt < 5; attempt++)
			try
			{
				using var response = await HttpClient.GetAsync(settings.SchemaListUrl, cancellation);

				if (response.IsSuccessStatusCode)
				{
					_logger.Info($"Vitrivr health check succeeded. Attempt={attempt + 1}.");
					return true;
				}

				_logger.Warning($"Vitrivr health check returned an unsuccessful status. Attempt={attempt + 1}, StatusCode={(int)response.StatusCode}.");
			}
			catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
			{
				_logger.Info("Vitrivr health check cancelled successfully.");
				throw;
			}

			catch (Exception exception)
			{
				_logger.Warning($"Vitrivr health check attempt failed. Attempt={attempt + 1}, ErrorType={exception.GetType().Name}.");
			}

		_logger.Warning("Vitrivr health check exhausted all attempts.");
		return false;
	}
}
