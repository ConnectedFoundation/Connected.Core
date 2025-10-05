using System.Text;

namespace Connected.Core.Mock;
public static class ValueGenerator
{
	private static readonly char[] DefaultCharacters = ['a', 'b', 'c', 'd'];

	private static Random Random { get; } = new();
	public static string Generate(int length)
	{
		var result = new StringBuilder();

		for (var i = 0; i < length; i++)
			result.Append(DefaultCharacters[Random.Next(0, DefaultCharacters.Length)]);

		return result.ToString();
	}
}
