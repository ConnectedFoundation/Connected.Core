using Connected.Configuration.Authentication;
using Connected.Identities.TwoFactor;
using Connected.Services;

namespace Connected.Identities;

public static class TwoFactorExtensions
{
	/// <summary>
	/// The length in bytes of the key that encrypts two-factor secrets.
	/// </summary>
	public const int KeyLength = 32;

	/// <summary>
	/// Reads the key that encrypts two-factor secrets, as <see cref="ITwoFactorConfiguration.Key"/> holds it.
	/// </summary>
	/// <param name="value">The key, encoded in Base64.</param>
	/// <param name="key">The key's <see cref="KeyLength"/> bytes, or an empty array when the value is not such a key.</param>
	/// <returns><see langword="true"/> when the value is a 256-bit key encoded in Base64.</returns>
	public static bool TryParseKey(string? value, out byte[] key)
	{
		var buffer = new byte[KeyLength];

		if (value is null || !Convert.TryFromBase64String(value, buffer, out var written) || written != KeyLength)
		{
			key = [];

			return false;
		}

		key = buffer;

		return true;
	}

	/// <summary>
	/// Determines whether a user must complete the sign-in with a code.
	/// </summary>
	/// <param name="service">The two-factor service.</param>
	/// <param name="configuration">The two-factor configuration.</param>
	/// <param name="user">The user who entered valid credentials.</param>
	/// <returns>
	/// <see langword="true"/> when two-factor sign-in is required for every user or the user has enabled it; such a user
	/// cannot sign in with credentials alone.
	/// </returns>
	public static async Task<bool> IsTwoFactorRequired(this ITwoFactorService service, ITwoFactorConfiguration configuration, IUser user)
	{
		if (configuration.Required)
			return true;

		/*
		 * Without a key no user can have set up two-factor sign-in, so the store is not read.
		 */
		if (string.IsNullOrWhiteSpace(configuration.Key))
			return false;

		return await service.Status(new PrimaryKeyDto<long> { Id = user.Id }) == TwoFactorStatus.Enabled;
	}
}
