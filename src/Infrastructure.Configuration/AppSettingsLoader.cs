using System.Text.Json;

namespace Infrastructure.Configuration;

public static class AppSettingsLoader
{
    public static AppSettings LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new AppSettings();
        
        return JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions{PropertyNameCaseInsensitive = true}) 
               ?? new AppSettings();
    }
}