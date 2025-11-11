namespace Connected.Authentication;
/// <summary>
/// A default base implementation of an authentication provider that integrates with
/// the middleware pipeline. It prepares the DTO and delegates to an overridable hook
/// for concrete providers to implement authentication behavior.
/// </summary>
public abstract class AuthenticationProvider
	: Middleware, IAuthenticationProvider
{
	/// <summary>
	/// Gets the current authentication DTO being processed by this provider.
	/// This is assigned in <see cref="Invoke(IAuthenticateDto)"/> before invoking the hook.
	/// </summary>
	protected IAuthenticateDto Dto { get; private set; } = default!;

	/// <summary>
	/// Entry point invoked by the authentication middleware to perform a provider-specific
	/// authentication process using the supplied DTO.
	/// </summary>
	/// <param name="dto">The authentication request DTO containing data such as schema and token.</param>
	/// <returns>A task that completes when the provider has finished processing.</returns>
	public async Task Invoke(IAuthenticateDto dto)
	{
		/*
		 * Capture the DTO for use by the provider and any derived implementations.
		 * Then delegate to the overridable hook where concrete authentication logic
		 * should be implemented by subclasses.
		 */
		Dto = dto;
		await OnInvoke();
	}

	/// <summary>
	/// Overridable hook that performs the provider's authentication logic after the
	/// <see cref="Dto"/> has been initialized.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	protected virtual async Task OnInvoke()
	{
		/*
		 * Default implementation is a no-op. Override in derived providers to apply
		 * authentication rules and to update any context or services as needed.
		 */
		await Task.CompletedTask;
	}
}