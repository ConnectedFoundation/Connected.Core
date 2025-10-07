using System.Text;

namespace Connected.Reflection;

public static class StringExtensions
{
	static StringExtensions()
	{
		UrlCharacteReplacements = new Dictionary<char, string>()
		{
			{'$', string.Empty},
			{'%', string.Empty},
			{'#', string.Empty},
			{'@', string.Empty},
			{'!', string.Empty},
			{'*', string.Empty},
			{'?', string.Empty},
			{';', string.Empty},
			{':', string.Empty},
			{'~', string.Empty},
			{'+', string.Empty},
			{'=', string.Empty},
			{'`', string.Empty},
			{'(', string.Empty},
			{')', string.Empty},
			{'[', string.Empty},
			{']', string.Empty},
			{'{', string.Empty},
			{'}', string.Empty},
			{'|', string.Empty},
			{'\\', string.Empty},
			{'\'', string.Empty},
			{'<', string.Empty},
			{'>', string.Empty},
			{',', string.Empty},
			{'/', string.Empty},
			{'^', string.Empty},
			{'&', string.Empty},
			{'"', string.Empty},
			{'.', string.Empty},
			{'č', "c"},
			{'š', "s"},
			{'ž', "z"},
			{'đ', "d"},
			{'ć', "c"}
		};
	}

	private static Dictionary<char, string> UrlCharacteReplacements { get; }

	public static string ToUrlString(this string value)
	{
		return FormatUrl(value);
	}

	public static string ToUrlString(this string value, Func<string, bool> checkExisting)
	{
		var formatted = FormatUrl(value);
		var current = formatted;
		var index = 0;

		while (checkExisting(current))
		{
			index++;
			current = $"{formatted}-{index}";
		}

		return current;
	}

	private static string FormatUrl(string value)
	{
		value = value.ToLowerInvariant();
		var result = new StringBuilder();

		foreach (var c in value)
		{
			if (UrlCharacteReplacements.TryGetValue(c, out string? replacement))
			{
				if (string.IsNullOrEmpty(replacement))
					continue;

				result.Append(replacement);
			}
			else
				result.Append(c);
		}

		return result.ToString();
	}

	public static string ToCamelCase(this string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return value;

		return $"{char.ToLowerInvariant(value[0])}{value.Substring(1)}";
	}

	public static string ToPascalCase(this string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return value;

		return $"{char.ToUpperInvariant(value[0])}{value.Substring(1)}";
	}
}