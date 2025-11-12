using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Authentication;
/// <summary>
/// Provides authentication related extension methods that simplify switching
/// the current execution scope to use a system identity. These helpers wrap
/// common boilerplate required to construct and submit an identity update.
/// </summary>
public static class AuthenticationExtensions
{
	/// <summary>
	/// Updates the identity within the provided asynchronous service scope to a
	/// system identity, returning the original scope for fluent chaining.
	/// </summary>
	/// <param name="scope">The active asynchronous service scope.</param>
	/// <returns>The original <see cref="AsyncServiceScope"/> after update.</returns>
	/// <exception cref="InvalidOperationException">Thrown when required services are missing.</exception>
	public static async Task<AsyncServiceScope> WithSystemIdentity(this AsyncServiceScope scope)
	{
		/*
		 * Resolve the authentication service from the scope and create a DTO used to
		 * update identity. The DTO is populated with a SystemIdentity instance then
		 * passed to the service for persistence. Finally return the scope to enable
		 * fluent usage in calling code.
		 */
		var service = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
		var dto = Dto.Factory.Create<IUpdateIdentityDto>();
		/*
		 * Assign a new system identity representing an internal authenticated context.
		 */
		dto.Identity = new SystemIdentity();
		/*
		 * Perform the asynchronous identity update operation.
		 */
		await service.UpdateIdentity(dto);
		/*
		 * Return the unchanged scope to allow method chaining (e.g. configuring other services).
		 */
		return scope;
	}
	/// <summary>
	/// Updates the identity of the provided authentication service instance to a
	/// system identity without returning the service.
	/// </summary>
	/// <param name="authentication">The authentication service whose identity is updated.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public static async Task WithSystemIdentity(this IAuthenticationService authentication)
	{
		/*
		 * Create an identity update DTO then set its Identity property to a new
		 * SystemIdentity to represent an elevated/system context.
		 */
		var dto = Dto.Factory.Create<IUpdateIdentityDto>();
		/*
		 * Populate the DTO with the system identity instance.
		 */
		dto.Identity = new SystemIdentity();
		/*
		 * Execute the asynchronous update, applying the new identity to the current scope.
		 */
		await authentication.UpdateIdentity(dto);
	}
}
