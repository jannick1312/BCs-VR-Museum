using System.Text.Json;

namespace Server;

public static class ServerRequestFactory
{
    public static string BuildRequestBody(string text, ServerMode mode)
    {
        if (mode == ServerMode.Deployed)
            return BuildDeployedRequest(text);
        return BuildStreamedRequest(text);
    }
    
    public static string BuildRequestUrl(string currentServerUrl, ServerMode mode)
    {
        if (mode == ServerMode.Deployed)
            return ServerSettings.NormalizeBaseUrl(currentServerUrl) + "search_one";
        return currentServerUrl.Trim();
    }
    
    private static string BuildDeployedRequest(string text)
    {
        return JsonSerializer.Serialize(new
        {
            text = text
        });
    }
    
    private static string BuildStreamedRequest(string text)
    {
        var payload = new
        {
            inputs = new
            {
                txt = new
                {
                    type = "TEXT",
                    data = text
                }
            },
            operations = new
            {
                clip = new
                {
                    field = "clip",
                    inputs = new
                    {
                        input = "txt"
                    },
                    parameters = new
                    {
                        limit = "1"
                    }
                },
                filelookup = new
                {
                    factory = "FieldLookup",
                    inputs = new Dictionary<string, string>
                    {
                        ["in"] = "clip"
                    },
                    parameters = new
                    {
                        field = "file",
                        keys = "path"
                    }
                }
            },
            output = "filelookup"
        };
        return JsonSerializer.Serialize(payload);
    }
}