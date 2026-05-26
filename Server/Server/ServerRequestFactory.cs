using System.Text.Json;

namespace Server;

public static class ServerRequestFactory
{
    public static string BuildRequestBody(string text)
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