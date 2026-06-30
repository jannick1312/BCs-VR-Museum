using Application;
using Core;
using Logger;
using System.Text;

namespace Infrastructure.Vitrivr;

public class VitrivrSearchService(VitrivrSettings settings) : ISearchEngine
{
	private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
	private readonly EventLogger _logger = new(nameof(VitrivrSearchService));

	public async Task<SearchResult> SearchAsync(SearchQuery query)
	{
		try
		{
			var json = VitrivrRequestFactory.BuildRequestBody(query);

			using var content = new StringContent(json, Encoding.UTF8, "application/json");

			using var response = await _httpClient.PostAsync(settings.QueryUrl, content);

			var responseText = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
			{
				_logger.Warning($"Vitrivr request failed with HTTP {(int)response.StatusCode}.");
				return SearchResult.Failure($"Vitrivr request failed with HTTP {(int)response.StatusCode}.");
			}

			var result = VitrivrResponseParser.Parse(responseText, settings.MediaFolderPath, settings.MediaBaseUrl);

			_logger.Info("Vitrivr request completed successfully.");
			return result;
		}
		catch (TaskCanceledException)
		{
			_logger.Warning("Vitrivr request timed out after 5 seconds.");
			return SearchResult.Failure("Vitrivr request timed out.");
		}
		catch (Exception exception)
		{
			_logger.Error("Vitrivr search failed unexpectedly", exception);
			return SearchResult.Failure(exception.Message);
		}
	}
}