using System.Text;

namespace Connected.Collections;

/// <summary>
/// Provides helpers for creating URL-friendly string keys.
/// </summary>
/// <remarks>
/// Normalizes text by preserving allowed characters, replacing separators with dashes,
/// applying configured character replacements, collapsing duplicate dashes, and enforcing lowercase output.
/// </remarks>
public static class WebString
{
	/// <summary>
	/// Defines valid characters that can be copied directly to the resulting key.
	/// </summary>
	private const string ValidCharacters = "abcdefghijklmnopqrstuvzxyw-";

	/// <summary>
	/// Defines characters that are converted to dash separators in the resulting key.
	/// </summary>
	private const string MinusReplacements = "_ .\t\r\n";

	/// <summary>
	/// Initializes static replacement mappings used during key normalization.
	/// </summary>
	static WebString()
	{
		/*
		  * Initialize culture-specific replacement mappings used when unsupported
		  * characters should be converted to ASCII alternatives.
		  */
		Replacements = new(StringComparer.OrdinalIgnoreCase)
		{
			{"č", "c"},
			{"š", "s"},
			{"ž", "z"},
			{"đ", "d"},
			{"ć", "c"}
		};
	}

	/// <summary>
	/// Gets custom character replacements used when source characters are not directly valid.
	/// </summary>
	private static Dictionary<string, string> Replacements { get; }

	/// <summary>
	/// Creates a URL-friendly key from source text.
	/// </summary>
	/// <param name="text">The source text to normalize.</param>
	/// <returns>A normalized key value.</returns>
	/// <exception cref="NullReferenceException">Thrown when <paramref name="text"/> is null or empty.</exception>
	public static string Create(string text)
	{
		/*
		 * Ensure source text exists before normalization starts.
		 */
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

		/*
		 * Collapse consecutive dashes into a single dash.
		 */
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

	/// <summary>
	/// Creates a URL-friendly key and ensures uniqueness against existing keys.
	/// </summary>
	/// <param name="text">The source text to normalize.</param>
	/// <param name="existing">The set of existing keys used for uniqueness checks.</param>
	/// <returns>A normalized unique key value.</returns>
	public static string Create(string text, IEnumerable<string?> existing)
	{
		/*
		 * Create the base normalized key.
		 */
		var prepared = Create(text);

		/*
		 * Return the base value immediately when no existing values are provided.
		 */
		if (!existing.Any())
			return prepared;

		/*
		 * Incrementally append suffix values until a unique key is found.
		 */
		var idx = 0;

		while (true)
		{
			var key = $"{prepared}-{idx}";

			/*
			 * Return the first candidate that is not present in the existing set.
			 */
			if (!existing.Contains(key, StringComparer.OrdinalIgnoreCase))
				return key;

			/*
			 * Continue searching with the next numeric suffix.
			 */
			idx++;
		}
	}
}
