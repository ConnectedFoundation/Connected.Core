
using Connected.Authorization.Dtos;

namespace Connected.Authorization;

public abstract class AuthorizationDecorationHandler : Middleware, IAuthorizationDecorationHandler
{
	protected IAuthorizationDecorationHandlerDto Dto { get; private set; } = default!;

	public async Task<AuthorizationResult> Invoke(IAuthorizationDecorationHandlerDto dto)
	{
		Dto = dto;

		return await OnInvoke();
	}

	protected virtual async Task<AuthorizationResult> OnInvoke()
	{
		return await Task.FromResult(AuthorizationResult.Skip);
	}
}
