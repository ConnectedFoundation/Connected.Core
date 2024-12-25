using Connected.Services;

namespace Connected.Authorization.Services;

public abstract class ServiceAuthorization : AuthorizationMiddleware, IServiceAuthorization
{
	protected IDto? Dto { get; private set; }
	protected ICallerContext? Caller { get; private set; }

	public async Task Invoke(IServiceAuthorizationDto dto)
	{
		Dto = dto.Dto;
		Caller = dto.Caller;

		await OnInvoke();
	}

	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}
}