using System.Text.Json.Serialization;

namespace Infrastructure.Vitrivr;

/// <summary>
/// Represents descriptor values returned by Vitrivr.
/// </summary>
public class Descriptors
{
	[JsonPropertyName("clip.vector")] public List<double?> ClipVector = [];

	[JsonPropertyName("file.path")] public string? FilePath;

	[JsonPropertyName("file.size")] public int? FileSize;

	[JsonPropertyName("time.end")] public long? TimeEnd;

	[JsonPropertyName("time.start")] public long? TimeStart;
}

/// <summary>
/// Represents the parent media item of a Vitrivr result.
/// </summary>
public class PartOf
{
	[JsonPropertyName("descriptors")] public Descriptors? Descriptors;

	[JsonPropertyName("id")] public string? Id;

	[JsonPropertyName("properties")] public Properties? Properties;

	[JsonPropertyName("relationship")] public Relationship? Relationship;

	[JsonPropertyName("score")] public double? Score;

	[JsonPropertyName("type")] public string? Type;
}

/// <summary>
/// Represents the properties of a Vitrivr result.
/// </summary>
public class Properties
{
}

/// <summary>
/// Represents the relationship to a parent Vitrivr result.
/// </summary>
public class Relationship
{
	[JsonPropertyName("partOf")] public PartOf? PartOf;
}

/// <summary>
/// Represents one search item from Vitrivr.
/// </summary>
public class Retrievable
{
	[JsonPropertyName("descriptors")] public Descriptors? Descriptors;

	[JsonPropertyName("id")] public string? Id;

	[JsonPropertyName("properties")] public Properties? Properties;

	[JsonPropertyName("relationship")] public Relationship? Relationship;

	[JsonPropertyName("score")] public double? Score;

	[JsonPropertyName("type")] public string? Type;
}

/// <summary>
/// Represents a Vitrivr search response.
/// </summary>
public class Root
{
	[JsonPropertyName("retrievables")] public List<Retrievable> Retrievables = [];
}



// These classes were generated from a Vitrivr response using https://json2csharp.com/. Codex helped adapt them to this project.
