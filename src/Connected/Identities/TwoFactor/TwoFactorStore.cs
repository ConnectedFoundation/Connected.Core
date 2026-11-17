using Connected.Entities;
using Connected.Storage;
using System.Collections.Immutable;

namespace Connected.Identities.TwoFactor;
/// <summary>
/// Reads and removes two-factor rows.
/// </summary>
/// <remarks>
/// The rows are read from storage and never cached, so every check sees the attempts and time steps written by the
/// sign-ins before it.
/// </remarks>
internal static class TwoFactorStore
{
	/// <summary>
	/// Returns a user's secret.
	/// </summary>
	public static async Task<UserTwoFactor?> Factor(IStorageProvider storage, long user)
	{
		return await storage.Open<UserTwoFactor>().AsEntity(f => f.User == user);
	}

	/// <summary>
	/// Returns the open challenge with the given value.
	/// </summary>
	/// <returns>The challenge, or <see langword="null"/> when it is unknown or expired.</returns>
	public static async Task<TwoFactorChallenge?> Challenge(IStorageProvider storage, string? value)
	{
		if (string.IsNullOrEmpty(value))
			return null;

		var hash = TwoFactorSecrets.Hash(value);
		var challenge = await storage.Open<TwoFactorChallenge>().AsEntity(f => f.Hash == hash);

		return challenge is null || challenge.Expire <= DateTimeOffset.UtcNow ? null : challenge;
	}

	/// <summary>
	/// Removes a user's challenges.
	/// </summary>
	public static async Task DeleteChallenges(IStorageProvider storage, long user)
	{
		var challenges = await storage.Open<TwoFactorChallenge>().AsEntities<TwoFactorChallenge>(f => f.User == user);

		foreach (var challenge in challenges)
			await Delete(storage, challenge);
	}

	/// <summary>
	/// Removes a challenge.
	/// </summary>
	public static async Task Delete(IStorageProvider storage, TwoFactorChallenge challenge)
	{
		await storage.Open<TwoFactorChallenge>().Update(challenge with { State = State.Delete });
	}
}
