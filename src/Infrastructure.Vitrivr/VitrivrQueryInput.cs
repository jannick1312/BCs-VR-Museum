using Core;

namespace Infrastructure.Vitrivr;

internal sealed class VitrivrQueryInput(string name, string clipInputName, string type, object data, string descriptorKey)
{
	public string Name { get; } = name;
	public string ClipInputName { get; } = clipInputName;
	public string DescriptorKey { get; } = descriptorKey;

	public static VitrivrQueryInput From(SearchQuery query)
	{
		return query switch
		{
			TextSearchQuery textQuery => new VitrivrQueryInput("txt", "txt", "TEXT", textQuery.Text, "descriptor"),
			VectorSearchQuery vectorQuery => new VitrivrQueryInput("vec", "vector", "FLOATVECTOR", vectorQuery.Vector, "vector"),
			_ => throw new ArgumentOutOfRangeException(nameof(query), "Unsupported search query type.")
		};
	}

	public Dictionary<string, object> ToInputs()
	{
		return new Dictionary<string, object> { [Name] = new { type, data } };
	}
}