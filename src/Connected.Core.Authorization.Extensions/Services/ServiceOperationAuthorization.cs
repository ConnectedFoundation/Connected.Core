using Connected.Services;

namespace Connected.Authorization.Services;

public abstract class ServiceOperationAuthorization<TDto> : AuthorizationMiddleware, IServiceOperationAuthorization<TDto>
	where TDto : IDto
{
	protected TDto? Dto { get; private set; }
	protected ICallerContext? Caller { get; private set; }

	public async Task<AuthorizationResult> Invoke(IServiceOperationAuthorizationDto<TDto> dto)
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