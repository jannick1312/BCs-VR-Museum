using System.Text.Json;
using Core;

namespace Infrastructure.Vitrivr;

public static class VitrivrRequestFactory
{
    public static string BuildRequestBody(SearchQuery query)
    {
        var payload = new
        {
            inputs = new
            {
                txt = new
                {
                    type = "TEXT",
                    data = query.Text
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
                        limit = query.Limit.ToString()
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