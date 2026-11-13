using Connected.Authentication;
using Connected.Configuration;
using Connected.Configuration.Authentication;
using Connected.Entities;
using Connected.Identities.Authentication;
using Connected.Identities.TwoFactor.Dtos;
using Connected.Services;
using Connected.Storage;

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

		var user = SetState(await users.Select(new PrimaryKeyDto<long> { Id = challenge.User }));

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

		var secret = TwoFactorSecrets.Unprotect(TwoFactorSecrets.Key(settings), user.Id, factor.Secret);

		if (secret is null || Totp.Match(secret, Dto.Code, DateTimeOffset.UtcNow, factor.LastStep) is not long step)
			return await Fail(challenge, settings);

		/*
		 * The row's ETag lets only one of two sign-ins with the same code, or with codes of neighbouring steps, through;
		 * the other fails on the update. The step is remembered, so the code cannot be used again.
		 */
		await storage.Open<UserTwoFactor>().Update(factor with
		{
			Status = TwoFactorStatus.Enabled,
			LastStep = step
		});

		await TwoFactorStore.Delete(storage, challenge);

		return await tokens.IssueSignInToken(configuration.Authentication.Tokens, user, challenge.Permanent);
	}

	/// <summary>
	/// Counts a wrong code against the challenge and removes the challenge when it has no attempts left.
	/// </summary>
	private async Task<string?> Fail(TwoFactorChallenge challenge, ITwoFactorConfiguration settings)
	{
		/*
		 * The attempts are added to the stored value, read again when another sign-in changed it first, so codes
		 * entered at the same time are all counted.
		 */
		var updated = await storage.Open<TwoFactorChallenge>().Update(challenge, f => Task.FromResult(f with { Attempts = f.Attempts + 1 }),
			async () => await storage.Open<TwoFactorChallenge>().AsEntity(f => f.Id == challenge.Id), Caller);

		var maxAttempts = settings.MaxAttempts > 0 ? settings.MaxAttempts : ITwoFactorConfiguration.DefaultMaxAttempts;

		if (updated is not null && updated.Attempts >= maxAttempts)
			await TwoFactorStore.Delete(storage, updated);

		return null;
	}
}
