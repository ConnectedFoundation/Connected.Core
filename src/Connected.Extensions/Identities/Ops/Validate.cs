using Connected.Authentication;
using Connected.Configuration;
using Connected.Identities.Authentication;
using Connected.Identities.Dtos;
using Connected.Identities.TwoFactor;
using Connected.Services;

namespace Connected.Identities.Ops;
internal sealed class Validate(IUserService users, IIdentityAuthenticationTokenService tokens, ITwoFactorService twoFactor, IAuthenticationService authentication,
	IConfigurationService configuration)
	 : ServiceFunction<IValidateUserDto, string?>
{
	protected override async Task<string?> OnInvoke()
	{
		await authentication.WithSystemIdentity();

		var user = await users.Select(Dto);

		if (user is null)
			return null;

		/*
		 * A user who must enter a code signs in with SignIn, which returns a challenge instead of a token.
		 */
		if (await twoFactor.IsTwoFactorRequired(configuration.Authentication.TwoFactor, user))
			return null;

		return await tokens.IssueSignInToken(configuration.Authentication.Tokens, user, Dto.Permanent);
	}
}
