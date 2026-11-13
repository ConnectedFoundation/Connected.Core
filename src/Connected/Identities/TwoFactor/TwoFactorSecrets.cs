using Connected.Configuration.Authentication;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Connected.Identities.TwoFactor;
/// <summary>
/// Encrypts the users' two-factor secrets and creates sign-in challenges.
/// </summary>
/// <remarks>
/// A secret is encrypted with AES-GCM under the configured key. The user's id is authenticated with it, so a secret
/// copied to another user's row cannot be decrypted.
/// </remarks>
internal static class TwoFactorSecrets
{
	private const int KeyLength = 32;
	private const int NonceLength = 12;
	private const int TagLength = 16;
	private const int ChallengeLength = 32;

	/// <summary>
	/// Returns the configured key.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when no 256-bit key is configured.</exception>
	public static byte[] Key(ITwoFactorConfiguration configuration)
	{
		var key = new byte[KeyLength];

		if (configuration.Key is null || !Convert.TryFromBase64String(configuration.Key, key, out var written) || written != KeyLength)
			throw new InvalidOperationException(SR.ErrTwoFactorKey);

		return key;
	}

	/// <summary>
	/// Encrypts a user's secret.
	/// </summary>
	/// <returns>The nonce, the encrypted secret and the tag, encoded in Base64.</returns>
	public static string Protect(byte[] key, long user, ReadOnlySpan<byte> secret)
	{
		var result = new byte[NonceLength + secret.Length + TagLength];
		var nonce = result.AsSpan(0, NonceLength);

		RandomNumberGenerator.Fill(nonce);

		using var aes = new AesGcm(key, TagLength);

		aes.Encrypt(nonce, secret, result.AsSpan(NonceLength, secret.Length), result.AsSpan(NonceLength + secret.Length), AssociatedData(user));

		return Convert.ToBase64String(result);
	}

	/// <summary>
	/// Decrypts a user's secret.
	/// </summary>
	/// <returns>The secret, or <see langword="null"/> when it was encrypted under another key or for another user.</returns>
	public static byte[]? Unprotect(byte[] key, long user, string value)
	{
		try
		{
			var data = Convert.FromBase64String(value);

			if (data.Length <= NonceLength + TagLength)
				return null;

			var secret = new byte[data.Length - NonceLength - TagLength];

			using var aes = new AesGcm(key, TagLength);

			aes.Decrypt(data.AsSpan(0, NonceLength), data.AsSpan(NonceLength, secret.Length), data.AsSpan(NonceLength + secret.Length), secret, AssociatedData(user));

			return secret;
		}
		catch (Exception ex) when (ex is FormatException or CryptographicException)
		{
			return null;
		}
	}

	/// <summary>
	/// Creates the value of a new sign-in challenge.
	/// </summary>
	public static string Challenge()
	{
		return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(ChallengeLength));
	}

	/// <summary>
	/// Returns the SHA-256 hash of a challenge in lowercase hexadecimal; the challenge itself is never stored.
	/// </summary>
	public static string Hash(string challenge)
	{
		return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(challenge)));
	}

	private static byte[] AssociatedData(long user)
	{
		var result = new byte[sizeof(long)];

		BinaryPrimitives.WriteInt64BigEndian(result, user);

		return result;
	}
}
