namespace Connected.Authorization.Net;

public abstract class HttpRequestAuthorization : AuthorizationMiddleware, IHttpRequestAuthorization
{
	public async Task<AuthorizationResult> Invoke()
	{
		return await OnInvoke();
	}

	protected virtual async Task<AuthorizationResult> OnInvoke()
	{
		return await Task.FromResult(AuthorizationResult.Skip);
	}
}