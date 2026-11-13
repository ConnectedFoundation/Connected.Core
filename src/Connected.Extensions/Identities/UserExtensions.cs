using Connected.Identities.Dtos;
using Connected.Identities.Ops;
using Connected.Identities.TwoFactor;
using Connected.Services;

namespace Connected.Identities;

internal sealed class UserExtensions(IServiceProvider services)
		  : Service(services), IUserExtensions
{
	public async Task<string?> Validate(IValidateUserDto dto)
	{
		return await Invoke(GetOperation<Validate>(), dto);
	}

	public async Task<SignInResult?> SignIn(IValidateUserDto dto)
	{
		return await Invoke(GetOperation<SignIn>(), dto);
	}
}
