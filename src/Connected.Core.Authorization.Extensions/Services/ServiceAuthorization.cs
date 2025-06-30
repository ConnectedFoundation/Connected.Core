using Connected.Services;

namespace Connected.Authorization.Services;

public abstract class ServiceAuthorization : AuthorizationMiddleware, IServiceAuthorization
{
	protected IDto Dto { get; private set; } = default!;
	protected ICallerContext Caller { get; private set; } = default!;

	public async Task<AuthorizationResult> Invoke(IServiceAuthorizationDto dto)
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