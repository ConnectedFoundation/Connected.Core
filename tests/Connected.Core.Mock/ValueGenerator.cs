using System.Text;

namespace Connected.Core.Mock;
public static class ValueGenerator
{
	private static readonly char[] DefaultCharacters = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm','n',
	'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'z', 'x', 'y', 'w'];

	private static Random Random { get; } = new();
	public static string Generate(int length)
	{
		var result = new StringBuilder();

		for (var i = 0; i < length; i++)
			result.Append(DefaultCharacters[Random.Next(0, DefaultCharacters.Length)]);

		return result.ToString();
	}
}
