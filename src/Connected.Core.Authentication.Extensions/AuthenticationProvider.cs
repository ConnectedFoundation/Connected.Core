namespace Connected.Authentication;
/// <summary>
/// A default implementation of the authentication provider.
/// </summary>
public abstract class AuthenticationProvider : MiddlewareComponent, IAuthenticationProvider
{
	protected IAuthenticateDto Dto { get; private set; } = default!;
	/// <inheritdoc cref="IAuthenticationProvider.Invoke"/>>
	public async Task Invoke(IAuthenticateDto dto)
	{
		Dto = dto;

		await OnInvoke();
	}
	/// <summary>
	/// This method is called by Invoke once the Dto has been set.
	/// </summary>
	/// <remarks>
	/// You should implement your authentication logic here.
	/// </remarks>
	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}
}