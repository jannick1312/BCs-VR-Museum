using System.Text.Json.Serialization;

namespace Infrastructure.Vitrivr;

public class Descriptors
{
	[JsonPropertyName("clip.vector")] public List<double?> ClipVector = [];

	[JsonPropertyName("file.path")] public string? FilePath;

	[JsonPropertyName("file.size")] public int? FileSize;

	[JsonPropertyName("time.end")] public long? TimeEnd;

	[JsonPropertyName("time.start")] public long? TimeStart;
}

public class PartOf
{
	[JsonPropertyName("descriptors")] public Descriptors? Descriptors;

	[JsonPropertyName("id")] public string? Id;

	[JsonPropertyName("properties")] public Properties? Properties;

	[JsonPropertyName("relationship")] public Relationship? Relationship;

	[JsonPropertyName("score")] public double? Score;

	[JsonPropertyName("type")] public string? Type;
}

public class Properties
{
}

public class Relationship
{
	[JsonPropertyName("partOf")] public PartOf? PartOf;
}

public class Retrievable
{
	[JsonPropertyName("descriptors")] public Descriptors? Descriptors;

	[JsonPropertyName("id")] public string? Id;

	[JsonPropertyName("properties")] public Properties? Properties;

	[JsonPropertyName("relationship")] public Relationship? Relationship;

	[JsonPropertyName("score")] public double? Score;

	[JsonPropertyName("type")] public string? Type;
}

public class Root
{
	[JsonPropertyName("retrievables")] public List<Retrievable> Retrievables = [];
}
