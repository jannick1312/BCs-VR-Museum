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
                    return true;
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                _logger.Warning("Vitrivr schema endpoint check failed.");
            }
        }

        return false;
    }
}