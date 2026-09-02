using Core;

namespace Infrastructure.Vitrivr;

/// <summary>
/// Represents the input settings for a Vitrivr search request.
/// </summary>
/// <param name="name">The key for the search value.</param>
/// <param name="clipInputName">The input name used by the search operation.</param>
/// <param name="type">The type of search value.</param>
/// <param name="data">The text or feature vector used for the search.</param>
/// <param name="descriptorKey">The descriptor requested from Vitrivr.</param>
internal sealed class VitrivrQueryInput(string name, string clipInputName, string type, object data, string descriptorKey)
{
	public string Name { get; } = name;
	public string ClipInputName { get; } = clipInputName;
	public string DescriptorKey { get; } = descriptorKey;

	/// <summary>
	/// Creates the Vitrivr input for a search.
	/// </summary>
	/// <param name="query">The search query to convert.</param>
	/// <returns>The matching Vitrivr input.</returns>
	public static VitrivrQueryInput From(SearchQuery query)
	{
		return query switch
		{
			TextSearchQuery textQuery => new VitrivrQueryInput("txt", "txt", "TEXT", textQuery.Text, "descriptor"),
			VectorSearchQuery vectorQuery => new VitrivrQueryInput("vec", "vector", "FLOATVECTOR", vectorQuery.Vector, "vector"),
			_ => throw new ArgumentOutOfRangeException(nameof(query), "Unsupported search query type.")
		};
	}

	/// <summary>
	/// Creates the input section of the Vitrivr request.
	/// </summary>
	/// <returns>The input data for the request.</returns>
	public Dictionary<string, object> ToInputs()
	{
		return new Dictionary<string, object> { [Name] = new { type, data } };
	}
}
