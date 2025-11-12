using System.Security.Cryptography;
using System.Text;

namespace Connected.Identities.Users;

internal static class UserUtils
{
	public static async Task<string?> HashPassword(string? value)
	{
		if (value is null)
			return null;

		using var md = MD5.Create();
		using var stream = new MemoryStream();

		stream.Write(Encoding.UTF8.GetBytes(value));
		stream.Seek(0, SeekOrigin.Begin);

		var hash = await md.ComputeHashAsync(stream);
		var text = new StringBuilder();

		for (var i = 0; i < hash.Length; i++)
			text.Append(hash[i].ToString("x2"));

		return text.ToString();
	}
}
