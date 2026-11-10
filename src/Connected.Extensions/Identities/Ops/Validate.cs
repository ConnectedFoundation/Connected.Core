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

		var token = await user.AuthenticationToken();

		/*
		 * A token that is about to reach the end of its maximum lifetime is not returned, since it could
		 * not be extended. Ensure replaces it with a new one.
		 */
		if (token is not null && await tokens.Select(DtoFactory.Create<IValueDto<string>>(f => f.Value = token)) is IIdentityAuthenticationToken existing && !configuration.Authentication.Tokens.IsSpent(existing))
			return token;

		return await tokens.Ensure(configuration.Authentication.Tokens, user.Token, Guid.NewGuid().ToString(), Dto.Permanent);
	}
}