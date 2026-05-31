using Application;
using Core;
using System.Text;

namespace Infrastructure.Vitrivr;

public class VitrivrSearchService(VitrivrSettings settings) : ISearchService
{
    private readonly HttpClient _httpClient = new() {Timeout = TimeSpan.FromSeconds(5)};

    public async Task<SearchResult> SearchAsync(SearchQuery query)
    {
        try
        {
            var json = VitrivrRequestFactory.BuildRequestBody(query);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(settings.QueryUrl, content);

            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return SearchResult.Failure($"Vitrivr request failed with HTTP {(int)response.StatusCode}.");
            
            return VitrivrResponseParser.Parse(responseText, settings.MediaFolderPath, settings.MediaBaseUrl);
        }
        catch (TaskCanceledException)
        {
            return SearchResult.Failure("Vitrivr request timed out.");
        }
        catch (Exception exception)
        {
            return SearchResult.Failure(exception.Message);
        }
    }
}