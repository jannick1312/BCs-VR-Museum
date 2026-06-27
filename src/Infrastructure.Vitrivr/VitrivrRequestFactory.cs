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
                        txt = "txt"
                    },
                    parameters = new
                    {
                        limit = query.Limit.ToString()
                    }
                },
                relations = new
                {
                    factory = "RelationExpander",
                    inputs = new Dictionary<string, string>
                    {
                        ["in"] = "clip"
                    },
                    parameters = new
                    {
                        outgoing = "partOf"
                    }
                },
                aggregator = new
                {
                    factory = "ScoreAggregator",
                    inputs = new Dictionary<string, string>
                    {
                        ["in"] = "relations"
                    }
                },
                timelookup = new
                {
                    factory = "FieldLookup",
                    inputs = new Dictionary<string, string>
                    {
                        ["in"] = "aggregator"
                    },
                    parameters = new
                    {
                        field = "time",
                        keys = "start, end"
                    }
                },
                filelookup = new
                {
                    factory = "ObjectFieldLookup",
                    inputs = new Dictionary<string, string>
                    {
                        ["in"] = "timelookup"
                    },
                    parameters = new
                    {
                        field = "file",
                        predicates = "partOf",
                        keys = "path"
                    }
                },
                filelookupextended = new
                {
                    factory = "FieldLookup",
                    inputs = new Dictionary<string, string>
                    {
                        ["in"] = "filelookup"
                    },
                    parameters = new
                    {
                        field = "file",
                        keys = "path"
                    }
                }
            },
            output = "filelookupextended"
        };

        return JsonSerializer.Serialize(payload);
    }
}