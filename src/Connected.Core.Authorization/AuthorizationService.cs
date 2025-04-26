using Connected.Services;

namespace Connected.Authorization;

internal sealed class AuthorizationService(IServiceProvider services)
	: Service(services), IAuthorizationService
{
	public Task<IAuthorizationResult> Authorize(IAuthorizationDto dto)
	{
		throw new NotImplementedException();
	}
}