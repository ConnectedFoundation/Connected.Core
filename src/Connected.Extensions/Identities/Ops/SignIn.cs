using Connected.Authentication;
using Connected.Configuration;
using Connected.Identities.Authentication;
using Connected.Identities.Dtos;
using Connected.Identities.TwoFactor;
using Connected.Identities.TwoFactor.Dtos;
using Connected.Services;

namespace Connected.Identities.Ops;
internal sealed class SignIn(IUserService users, IIdentityAuthenticationTokenService tokens, ITwoFactorService twoFactor, IAuthenticationService authentication,
	IConfigurationService configuration)
	 : ServiceFunction<IValidateUserDto, SignInResult?>
{
	protected override async Task<SignInResult?> OnInvoke()
	{
		await authentication.WithSystemIdentity();

		var user = await users.Select(Dto);

		if (user is null)
			return null;

		if (await twoFactor.IsTwoFactorRequired(configuration.Authentication.TwoFactor, user))
		{
			return await twoFactor.Challenge(DtoFactory.Create<IChallengeTwoFactorDto>(f =>
			{
				f.User = user.Id;
				f.Permanent = Dto.Permanent;
			}));
		}

		return new SignInResult
		{
			Stage = SignInStage.Completed,
			Token = await tokens.IssueSignInToken(configuration.Authentication.Tokens, user, Dto.Permanent)
		};
	}
}
