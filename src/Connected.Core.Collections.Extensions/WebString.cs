using System.Text;

namespace Connected.Collections;

public static class WebString
{
	private const string ValidCharacters = "abcdefghijklmnopqrstuvzxyw-";
	private const string MinusReplacements = "_ .\t\r\n";

	static WebString()
	{
		Replacements = new(StringComparer.OrdinalIgnoreCase)
		{
			{"č", "c"},
			{"š", "s"},
			{"ž", "z"},
			{"đ", "d"},
			{"ć", "c"}
		};
	}

	private static Dictionary<string, string> Replacements { get; }

	public static string Create(string text)
	{
		if (string.IsNullOrEmpty(text))
			throw new NullReferenceException(nameof(text));

		var result = new StringBuilder();
		/*
		 * Analyze each character.
		 */
		foreach (var s in text.Trim())
		{
			/*
			 * If it's in a ValidCharacters collection that's fine, we just copy it
			 * into the result.
			 */
			if (ValidCharacters.Contains(s, StringComparison.OrdinalIgnoreCase))
			{
				result.Append(s);
				continue;
			}
			/*
			 * Look if it's in the MinusReplacements collection which means we'll
			 * replace the character with the minus sign.
			 */
			if (MinusReplacements.Contains(s, StringComparison.OrdinalIgnoreCase))
			{
				result.Append('-');
				continue;
			}
			/*
			 * Last chance for the character to be included in the result is to 
			 * resolve the hardcoded character replacements.
			 */
			if (Replacements.TryGetValue(s.ToString(), out var replacement))
				result.Append(replacement);
		}
		/*
		 * It's possible that no characters are included in the result. If that's
		 * the case we are going to include an a character just for the sake of
		 * being a valid string.
		 */
		if (result.Length == 0)
			result.Append('a');
		/*
		 * Remove any redundant minus characters and we are done.
		 */
		var sb = new StringBuilder();
		var active = false;

		for (var i = 0; i < result.Length; i++)
		{
			if (result[i] == '-')
			{
				if (active)
					continue;

				active = true;
				sb.Append(result[i]);
			}
			else
			{
				active = false;
				sb.Append(result[i]);
			}
		}
		/*
		 * Remove trailing minus if exists.
		 */
		return sb.ToString().Trim().Trim('-').ToLowerInvariant();
	}

	public static string Create(string text, IEnumerable<string> existing)
	{
		var prepared = Create(text);

		if (!existing.Any())
			return prepared;

		var idx = 0;

		while (true)
		{
			var key = $"{prepared}-{idx}";

			if (!existing.Contains(key, StringComparer.OrdinalIgnoreCase))
				return key;

			idx++;
		}
	}
}
