using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Connected.Identities.TwoFactor;
/// <summary>
/// Computes and checks time-based one-time passwords (RFC 6238) with HMAC-SHA1, six digits and a 30-second step.
/// </summary>
internal static class Totp
{
	/// <summary>
	/// The number of seconds a code is valid for.
	/// </summary>
	public const int Period = 30;

	/// <summary>
	/// The number of digits of a code.
	/// </summary>
	public const int Digits = 6;

	/// <summary>
	/// The number of bytes of a new secret, the length of an HMAC-SHA1 hash as RFC 4226 recommends.
	/// </summary>
	public const int SecretLength = 20;

	private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

	/// <summary>
	/// Returns the time step of a point in time.
	/// </summary>
	public static long Step(DateTimeOffset time)
	{
		return time.ToUnixTimeSeconds() / Period;
	}

	/// <summary>
	/// Computes the code of a time step (RFC 4226, section 5.3).
	/// </summary>
	/// <param name="secret">The shared secret.</param>
	/// <param name="step">The time step.</param>
	/// <param name="digits">The number of digits of the code, from 6 to 9.</param>
	public static string Code(ReadOnlySpan<byte> secret, long step, int digits = Digits)
	{
		Span<byte> counter = stackalloc byte[sizeof(long)];
		Span<byte> hash = stackalloc byte[HMACSHA1.HashSizeInBytes];

		BinaryPrimitives.WriteInt64BigEndian(counter, step);
		HMACSHA1.HashData(secret, counter, hash);

		var offset = hash[^1] & 0x0f;
		var binary = BinaryPrimitives.ReadInt32BigEndian(hash[offset..]) & 0x7fffffff;
		var modulus = 1;

		for (var i = 0; i < digits; i++)
			modulus *= 10;

		return (binary % modulus).ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');
	}

	/// <summary>
	/// Finds the time step of a code that is valid now.
	/// </summary>
	/// <param name="secret">The shared secret.</param>
	/// <param name="code">The code entered by the user.</param>
	/// <param name="now">The current time.</param>
	/// <param name="lastStep">The time step of the last code the user signed in with; it and earlier steps are refused.</param>
	/// <returns>The matching step, or <see langword="null"/> when the code is not valid now or is already used.</returns>
	/// <remarks>
	/// The step before and after the current one are accepted too, for clocks that are off by up to 30 seconds and codes
	/// entered at the end of their step (RFC 6238, section 5.2).
	/// </remarks>
	public static long? Match(ReadOnlySpan<byte> secret, string? code, DateTimeOffset now, long lastStep)
	{
		if (code is null || code.Length != Digits || !code.All(char.IsAsciiDigit))
			return null;

		var current = Step(now);
		var entered = Encoding.ASCII.GetBytes(code);
		long? match = null;

		/*
		 * Every step is compared in constant time, so the time taken does not tell which step matched.
		 */
		for (var step = current - 1; step <= current + 1; step++)
		{
			if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Code(secret, step)), entered) && step > lastStep && match is null)
				match = step;
		}

		return match;
	}

	/// <summary>
	/// Encodes bytes in Base32 (RFC 4648) without padding, as authenticator apps expect secrets.
	/// </summary>
	public static string Base32(ReadOnlySpan<byte> data)
	{
		var result = new StringBuilder((data.Length * 8 + 4) / 5);
		var buffer = 0;
		var bits = 0;

		foreach (var value in data)
		{
			buffer = ((buffer << 8) | value) & 0xffff;
			bits += 8;

			while (bits >= 5)
			{
				bits -= 5;
				result.Append(Base32Alphabet[(buffer >> bits) & 0x1f]);
			}
		}

		if (bits > 0)
			result.Append(Base32Alphabet[(buffer << (5 - bits)) & 0x1f]);

		return result.ToString();
	}

	/// <summary>
	/// Returns the <c>otpauth://totp</c> URI of a secret, as authenticator apps read it from a QR code.
	/// </summary>
	/// <param name="issuer">The issuer shown next to the account.</param>
	/// <param name="account">The account name.</param>
	/// <param name="secret">The secret, encoded in Base32.</param>
	public static string Uri(string issuer, string account, string secret)
	{
		var label = $"{System.Uri.EscapeDataString(issuer)}:{System.Uri.EscapeDataString(account)}";

		return $"otpauth://totp/{label}?secret={secret}&issuer={System.Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={Digits}&period={Period}";
	}
}
