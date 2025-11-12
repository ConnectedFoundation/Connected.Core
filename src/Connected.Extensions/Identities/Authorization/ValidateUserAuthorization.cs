using Connected.Annotations;
using Connected.Authorization;
using Connected.Authorization.Services;
using Connected.Identities.Dtos;

namespace Connected.Identities.Authorization;

[Middleware<IUserExtensions>(nameof(IUserExtensions.Validate))]
internal sealed class ValidateUserAuthorization
	: ServiceOperationAuthorization<IValidateUserDto>
{
	protected override async Task<AuthorizationResult> OnInvoke()
	{
		return await Task.FromResult(AuthorizationResult.Pass);
	}
}
