using Connected.Reflection.Merging;
using System.Text;
using System.Text.Json;

namespace Connected.Reflection;

public static class Serializer
{
	private static readonly JsonSerializerOptions _options;

	static Serializer()
	{
		_options = new JsonSerializerOptions
		{
			AllowTrailingCommas = true,
			IncludeFields = false,
			IgnoreReadOnlyFields = false,
			IgnoreReadOnlyProperties = true,
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true
		};
	}

	internal static JsonSerializerOptions SerializerOptions => _options;

	public static async Task<T?> Deserialize<T>(string value)
	{
		using var ms = new MemoryStream(Encoding.UTF8.GetBytes(value));

		ms.Seek(0, SeekOrigin.Begin);

		return await JsonSerializer.DeserializeAsync<T>(ms, _options);
	}

	public static async Task<string?> Serialize(object value)
	{
		using var ms = new MemoryStream();

		await System.Text.Json.JsonSerializer.SerializeAsync(ms, value, JsonSerializerOptions.Default);

		ms.Seek(0, SeekOrigin.Begin);

		return Encoding.UTF8.GetString(ms.ToArray());
	}

	public static T Merge<T>(T destination, params object?[] sources)
	{
		if (destination is null)
			return destination;

		if (sources is null || !sources.Any())
			return destination;

		new ObjectMerger().Merge(destination, sources);

		return destination;
	}
}
