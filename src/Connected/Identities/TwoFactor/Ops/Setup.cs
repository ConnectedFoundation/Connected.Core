using Connected.Authentication;
using Connected.Configuration;
using Connected.Entities;
using Connected.Services;
using Connected.Storage;
using System.Security.Cryptography;

namespace Connected.Identities.TwoFactor.Ops;

internal sealed class Setup(IStorageProvider storage, IUserService users, IConfigurationService configuration, IAuthenticationService authentication)
	: ServiceFunction<IValueDto<string>, TwoFactorSetup?>
{
	/// <summary>
	/// The issuer shown by authenticator apps when none is configured.
	/// </summary>
	private const string DefaultIssuer = "Connected";

	protected override async Task<TwoFactorSetup?> OnInvoke()
	{
		/*
		 * The caller is anonymous; the challenge is the credential, and reading the user needs the system identity.
		 */
		await authentication.WithSystemIdentity();

		var challenge = await TwoFactorStore.Challenge(storage, Dto.Value);

		if (challenge is null || challenge.Stage != SignInStage.TwoFactorSetup)
			return null;

		var user = SetState(await users.Select(new PrimaryKeyDto<long> { Id = challenge.User }));

		if (user is null || user.Status != UserStatus.Active)
			return null;

		var settings = configuration.Authentication.TwoFactor;
		var key = TwoFactorSecrets.Key(settings);
		var factor = await TwoFactorStore.Factor(storage, user.Id);

		/*
		 * A confirmed secret is replaced only after a reset, never through a challenge.
		 */
		if (factor?.Status == TwoFactorStatus.Enabled)
			return null;

		var secret = RandomNumberGenerator.GetBytes(Totp.SecretLength);
		var encrypted = TwoFactorSecrets.Protect(key, user.Id, secret);

		if (factor is null)
		{
			await storage.Open<UserTwoFactor>().Update(new UserTwoFactor
			{
				State = State.Add,
				User = user.Id,
				Secret = encrypted,
				Status = TwoFactorStatus.Pending,
				Created = DateTimeOffset.UtcNow
			});
		}
		else
		{
			/*
			 * The row's ETag makes a setup that runs at the same time fail instead of leaving a secret the user never saw.
			 */
			await storage.Open<UserTwoFactor>().Update(factor with
			{
				Secret = encrypted,
				LastStep = 0,
				Created = DateTimeOffset.UtcNow
			});
		}

		var encoded = Totp.Base32(secret);
		var issuer = string.IsNullOrWhiteSpace(settings.Issuer) ? DefaultIssuer : settings.Issuer;

		return new TwoFactorSetup
		{
			Secret = encoded,
			Uri = Totp.Uri(issuer, user.Email ?? user.Token, encoded)
		};
	}
}
