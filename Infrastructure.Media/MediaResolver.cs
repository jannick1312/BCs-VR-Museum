using Application;
using Core;

namespace Infrastructure.Media;

public class MediaResolver : IMediaLoader
{
    private readonly HttpClient _httpClient = new() {Timeout = TimeSpan.FromSeconds(5)};

    public static bool IsLocal(SearchResultItem item)
    {
        return File.Exists(item.LocalPath);
    }

    public async Task<MediaContent> LoadAsync(SearchResultItem item)
    {
        try
        {
            if (File.Exists(item.LocalPath))
            {
                var localBytes = await File.ReadAllBytesAsync(item.LocalPath);
                return MediaContent.FromBytes(localBytes, item.LocalPath);
            }

            var remoteBytes = await _httpClient.GetByteArrayAsync(item.RemoteUrl);
            return MediaContent.FromBytes(remoteBytes, item.RemoteUrl);
        }
        catch (TaskCanceledException)
        {
            return MediaContent.Failure("Media request timed out.");
        }
        catch (Exception exception)
        {
            return MediaContent.Failure(exception.Message);
        }
    }
}