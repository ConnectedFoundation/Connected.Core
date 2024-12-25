using Connected.Services;

namespace Connected.Authorization.Services;

public abstract class ServiceOperationAuthorization<TDto> : AuthorizationMiddleware, IServiceOperationAuthorization<TDto>
	where TDto : IDto
{
	protected IDto? Dto { get; private set; }
	protected ICallerContext? Caller { get; private set; }

	public async Task Invoke(IServiceOperationAuthorizationDto<TDto> dto)
	{
		Dto = dto.Dto;
		Caller = dto.Caller;

		await OnInvoke();
	}

	protected virtual Task OnInvoke()
	{
		return Task.CompletedTask;
	}
}