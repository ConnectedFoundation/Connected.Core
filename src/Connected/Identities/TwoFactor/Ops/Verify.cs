using Connected.Authentication;
using Connected.Configuration;
using Connected.Configuration.Authentication;
using Connected.Entities;
using Connected.Identities.Authentication;
using Connected.Identities.TwoFactor.Dtos;
using Connected.Services;
using Connected.Storage;
using System.Data;

namespace Connected.Identities.TwoFactor.Ops;

internal sealed class Verify(IStorageProvider storage, IUserService users, IIdentityAuthenticationTokenService tokens, IConfigurationService configuration,
	IAuthenticationService authentication)
	: ServiceFunction<IVerifyTwoFactorDto, string?>
{
	protected override async Task<string?> OnInvoke()
	{
		/*
		 * The caller is anonymous; the challenge is the credential, and reading the user and issuing the token need the
		 * system identity.
		 */
		await authentication.WithSystemIdentity();

		var challenge = await TwoFactorStore.Challenge(storage, Dto.Challenge);

		if (challenge is null)
			return null;

		var user = await users.Select(new PrimaryKeyDto<long> { Id = challenge.User });

		if (user is null || user.Status != UserStatus.Active)
			return null;

		var settings = configuration.Authentication.TwoFactor;
		var factor = await TwoFactorStore.Factor(storage, user.Id);
		var expected = challenge.Stage == SignInStage.TwoFactor ? TwoFactorStatus.Enabled : TwoFactorStatus.Pending;

		/*
		 * A setup challenge whose secret was not requested yet has no code to check.
		 */
		if (factor is null || factor.Status != expected)
			return null;

		/*
		 * From here the code is checked, so a middleware that finds the user in the state can count a wrong code.
		 */
		SetState(user);

		var now = DateTimeOffset.UtcNow;

		if (await Reserve(factor, now) is not UserTwoFactor current || current.BlockedUntil > now || current.Status != expected)
			return null;

		var secret = TwoFactorSecrets.Unprotect(TwoFactorSecrets.Key(settings), user.Id, current.Secret);

		if (secret is null || Totp.Match(secret, Dto.Code, now, current.LastStep) is not long step)
		{
			var maxAttempts = settings.MaxAttempts > 0 ? settings.MaxAttempts : ITwoFactorConfiguration.DefaultMaxAttempts;
			var lockout = settings.LockoutDuration > 0 ? settings.LockoutDuration : ITwoFactorConfiguration.DefaultLockoutDuration;

			if (current.Failures >= maxAttempts)
			{
				await storage.Open<UserTwoFactor>().Update(current with
				{
					Failures = 0,
					BlockedUntil = now.AddMinutes(lockout)
				});
			}

			return null;
		}

		/*
		 * The step is remembered, so the code cannot be used again, and the count of codes starts over.
		 */
		await storage.Open<UserTwoFactor>().Update(current with
		{
			Status = TwoFactorStatus.Enabled,
			LastStep = step,
			Failures = 0,
			BlockedUntil = null
		});

		await TwoFactorStore.Delete(storage, challenge);

		return await tokens.IssueSignInToken(configuration.Authentication.Tokens, user, challenge.Permanent);
	}

	/// <summary>
	/// Counts the code against the user before it is checked.
	/// </summary>
	/// <returns>The user's secret as it is now, or <see langword="null"/> when it was removed meanwhile.</returns>
	/// <remarks>
	/// <para>
	/// The count is added to the stored value, read again when another sign-in changed it first. The update locks the row
	/// until the sign-in's transaction ends, so codes entered at the same time are checked one after another, each against
	/// the count, lockout and last used step the previous one left: a burst of codes cannot exceed the limit, and of two
	/// sign-ins with the same code only the first gets through.
	/// </para>
	/// <para>
	/// During a lockout the count is not raised, so it starts over when the lockout ends.
	/// </para>
	/// </remarks>
	private async Task<UserTwoFactor?> Reserve(UserTwoFactor factor, DateTimeOffset now)
	{
		try
		{
			return await storage.Open<UserTwoFactor>().Update(factor, f => Task.FromResult(f.BlockedUntil > now ? f : f with { Failures = f.Failures + 1 }),
				async () => await TwoFactorStore.Factor(storage, factor.User) ?? factor, Caller);
		}
		catch (DBConcurrencyException)
		{
			/*
			 * The secret was removed, for example by a reset, while the code was being entered.
			 */
			return null;
		}
	}
}
