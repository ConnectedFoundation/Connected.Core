using Connected.Configuration;
using Connected.Entities;
using Connected.Identities.TwoFactor.Dtos;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.TwoFactor.Ops;

internal sealed class Challenge(IStorageProvider storage, IUserService users, IConfigurationService configuration)
	: ServiceFunction<IChallengeTwoFactorDto, UserSignInResult>
{
	protected override async Task<UserSignInResult> OnInvoke()
	{
		var settings = configuration.Authentication.TwoFactor;

		/*
		 * A missing key is reported when the sign-in starts, not after the user has entered a code.
		 */
		TwoFactorSecrets.Key(settings);

		var user = SetState(await users.Select(new PrimaryKeyDto<long> { Id = Dto.User })) ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);
		var factor = await TwoFactorStore.Factor(storage, user.Id);
		var stage = factor?.Status == TwoFactorStatus.Enabled ? SignInStage.TwoFactor : SignInStage.TwoFactorSetup;

		/*
		 * Only the latest sign-in's challenge works. Wrong codes are counted per user, not per challenge, so starting the
		 * sign-in again does not add attempts to guess a code.
		 */
		await TwoFactorStore.DeleteChallenges(storage, user.Id);

		var value = TwoFactorSecrets.Challenge();
		var expire = DateTimeOffset.UtcNow.AddMinutes(settings.ChallengeLifetime);

		await storage.Open<TwoFactorChallenge>().Update(new TwoFactorChallenge
		{
			State = State.Add,
			User = user.Id,
			Hash = TwoFactorSecrets.Hash(value),
			Stage = stage,
			Expire = expire,
			Permanent = Dto.Permanent
		});

		return new UserSignInResult
		{
			Stage = stage,
			Challenge = value,
			Expire = expire
		};
	}
}
