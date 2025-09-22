using Connected.Services;

namespace Connected.Authorization.Services;
public abstract class ScopeAuthorization : AuthorizationMiddleware, IScopeAuthorization
{
	protected IDto Dto { get; private set; } = default!;
	protected ICallerContext Caller { get; private set; } = default!;

	public async Task<AuthorizationResult> Invoke(IScopeAuthorizationDto dto)
	{
		Dto = dto.Dto;
		Caller = dto.Caller;

		return await OnInvoke();
	}

	protected virtual async Task<AuthorizationResult> OnInvoke()
	{
		return await Task.FromResult(AuthorizationResult.Skip);
	}
}
