namespace Connected.Authorization.Net;

public abstract class HttpRequestAuthorization : AuthorizationMiddleware, IHttpRequestAuthorization
{
	public async Task Invoke()
	{
		await OnInvoke();
	}

	protected virtual Task OnInvoke()
	{
		return Task.CompletedTask;
	}
}