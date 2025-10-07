using System.Text.Json;
using System.Text.Json.Nodes;

namespace Connected.Reflection;

public static class JsonExtensions
{
	public static IDictionary<string, object?> ToDictionary(this JsonNode node)
	{
		var result = new Dictionary<string, object?>();

		if (node is JsonObject obj)
		{
			foreach (var property in obj)
				DeserializeJson(property, result);
		}

		return result;
	}

	private static void DeserializeJson(KeyValuePair<string, JsonNode?> node, Dictionary<string, object?> items)
	{
		var value = DeserializeNode(node.Value);

		items.Add(node.Key, value);
	}

	private static object? DeserializeNode(JsonNode? node)
	{
		if (node is null)
			return null;

		if (node is JsonArray array)
			return DeserializeArray(array);
		else if (node is JsonObject obj)
			return DeserializeObject(obj);
		else if (node is JsonValue value)
		{
			var element = value.GetValue<JsonElement>();

			switch (element.ValueKind)
			{
				case JsonValueKind.Undefined:
					return null;
				case JsonValueKind.False:
					return false;
				case JsonValueKind.Null:
					return null;
				case JsonValueKind.Number:
					return element.GetDouble();
				case JsonValueKind.True:
					return true;
				case JsonValueKind.String:
					return element.GetString();
				default:
					return null;
			}
		}

		return null;
	}

	private static object? DeserializeObject(JsonObject value)
	{
		var result = new Dictionary<string, object?>();

		foreach (var property in value)
			DeserializeJson(property, result);

		return result;
	}

	private static object? DeserializeArray(JsonArray value)
	{
		var result = new List<object?>();

		foreach (var element in value)
			result.Add(DeserializeNode(element));

		return result;
	}
}