using Application;
using Logger;

namespace Infrastructure.Vitrivr;

public class VitrivrServerHealthService(VitrivrSettings settings) : IServerHealthService
{
	private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(1) };

	private readonly EventLogger _logger = new(nameof(VitrivrServerHealthService));

	public async Task<bool> IsReachableAsync()
	{
		for (var attempt = 0; attempt < 5; attempt++)
		{
			try
			{
				using var response = await _httpClient.GetAsync(settings.SchemaListUrl);

				if (response.IsSuccessStatusCode)
				{
					_logger.Info($"Vitrivr health check succeeded. Attempt={attempt + 1}.");
					return true;
				}

				_logger.Warning($"Vitrivr health check returned an unsuccessful status. Attempt={attempt + 1}, StatusCode={(int)response.StatusCode}.");
			}
			catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
			{
				_logger.Warning($"Vitrivr health check failed. ErrorType={exception.GetType().Name}.");
			}
		}

		_logger.Warning("Vitrivr health check exhausted all attempts.");
		return false;
	}
}