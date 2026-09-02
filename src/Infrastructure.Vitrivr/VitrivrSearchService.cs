using System.Text;
using Application;
using Core;
using Logger;

namespace Infrastructure.Vitrivr;

/// <summary>
/// Runs media searches on a Vitrivr server.
/// </summary>
/// <param name="settings">The settings used for Vitrivr searches.</param>
public class VitrivrSearchService(VitrivrSettings settings) : ISearchEngine
{
	private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
	private readonly EventLogger _logger = new(nameof(VitrivrSearchService));

	/// <summary>
	/// Sends a media search to the Vitrivr server.
	/// </summary>
	/// <param name="query">The search to send.</param>
	/// <returns>A task containing the search result from the server.</returns>
	public async Task<SearchResult> SearchAsync(SearchQuery query)
	{
		try
		{
			var json = VitrivrRequestFactory.BuildRequestBody(query);

			using var content = new StringContent(json, Encoding.UTF8, "application/json");

			using var response = await HttpClient.PostAsync(settings.QueryUrl, content);

			var responseText = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
			{
				_logger.Warning($"Vitrivr search request failed. StatusCode={(int)response.StatusCode}.");
				return SearchResult.Failure($"Vitrivr request failed with HTTP {(int)response.StatusCode}.");
			}

			var result = VitrivrResponseParser.Parse(responseText, settings.MediaFolderPath, settings.MediaBaseUrl);

			if (result.Success)
				_logger.Info($"Vitrivr search request completed. Items={result.Items.Count}.");
			return result;
		}
		catch (TaskCanceledException)
		{
			_logger.Warning("Vitrivr search request timed out.");
			return SearchResult.Failure("Vitrivr request timed out.");
		}
		catch (Exception exception)
		{
			_logger.Error("Vitrivr search request failed unexpectedly", exception);
			return SearchResult.Failure(exception.Message);
		}
	}
}
