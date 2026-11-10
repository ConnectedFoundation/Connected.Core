using Connected.Authentication;
using Connected.Configuration;
using Connected.Identities.Authentication;
using Connected.Identities.Dtos;
using Connected.Services;

namespace Connected.Identities.Ops;
internal sealed class Validate(IUserService users, IIdentityAuthenticationTokenService tokens, IAuthenticationService authentication, IConfigurationService configuration)
	 : ServiceFunction<IValidateUserDto, string?>
{
	protected override async Task<string?> OnInvoke()
	{
		await authentication.WithSystemIdentity();

		var user = await users.Select(Dto);

		if (user is null)
			return null;

		/*
		 * A token that is about to reach the end of its maximum lifetime is not returned, since it could
		 * not be extended. Ensure replaces it with a new one.
		 */
		var token = await user.SignInToken(configuration.Authentication.Tokens);

		if (token is not null)
			return token;

		return await tokens.Ensure(configuration.Authentication.Tokens, user.Token, Guid.NewGuid().ToString(), Dto.Permanent);
	}
}