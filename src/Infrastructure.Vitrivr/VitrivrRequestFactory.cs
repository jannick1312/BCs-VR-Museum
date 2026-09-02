using System.Text.Json;
using Core;

namespace Infrastructure.Vitrivr;

/// <summary>
/// Creates request bodies for Vitrivr searches.
/// </summary>
public static class VitrivrRequestFactory
{
	/// <summary>
	/// Creates the Vitrivr request body for a search query.
	/// </summary>
	/// <param name="query">The search query for the request.</param>
	/// <returns>The request text.</returns>
	public static string BuildRequestBody(SearchQuery query)
	{
		var input = VitrivrQueryInput.From(query);

		var payload = new
		{
			inputs = input.ToInputs(),
			operations = new
			{
				clip = new
				{
					field = "clip",
					inputs = new Dictionary<string, string>
					{
						[input.ClipInputName] = input.Name
					},
					parameters = new
					{
						limit = query.Limit.ToString(),
						returnDescriptor = "true"
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
				desclookup = new
				{
					factory = "FieldLookup",
					inputs = new Dictionary<string, string>
					{
						["in"] = "timelookup"
					},
					parameters = new
					{
						field = "clip",
						keys = input.DescriptorKey
					}
				},
				filelookup = new
				{
					factory = "ObjectFieldLookup",
					inputs = new Dictionary<string, string>
					{
						["in"] = "desclookup"
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
