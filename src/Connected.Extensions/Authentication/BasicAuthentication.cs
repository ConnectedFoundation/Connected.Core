using Connected.Identities;
using Connected.Identities.Dtos;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Authentication;

/// <summary>
/// Concrete implementation of <see cref="BasicAuthenticationProvider"/> that authenticates
/// a user via the parsed basic credentials (user name and password) and, upon success,
/// elevates execution to that user's identity within an isolated service scope.
/// </summary>
internal sealed class BasicAuthentication
	: BasicAuthenticationProvider
{
	/// <summary>
	/// Executes the basic authentication workflow: validates that credentials were parsed,
	/// resolves required services, loads the user by name/password and updates the ambient
	/// identity context if the user is found.
	/// </summary>
	/// <remarks>
	/// The method intentionally performs early returns for missing credentials or unknown users
	/// to avoid unnecessary service calls. All service resolution occurs from a temporary scope
	/// created with a system identity to guarantee sufficient privileges for lookups.
	/// </remarks>
	protected override async Task OnInvoke()
	{
		/*
		 * Ensure that both user name and password were extracted by the base provider.
		 * If either is missing, abort authentication without modifying the current identity.
		 */
		if (UserName is null || Password is null)
			return;

		/*
		 * Create a new service scope elevated to the system identity so we can perform
		 * secure user lookups and identity updates with the necessary privileges.
		 */
		using var scope = await Scope.Create().WithSystemIdentity();

		/*
		 * Resolve the authentication service (to apply identity changes), the user service
		 * (to retrieve user accounts), and the selection DTO used to query a user record.
		 */
		var authentication = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
		var users = scope.ServiceProvider.GetRequiredService<IUserService>();
		var dto = scope.ServiceProvider.GetRequiredService<ISelectUserDto>();

		/*
		 * Populate the selection DTO with the provided credentials for validation.
		 */
		dto.User = UserName;
		dto.Password = Password;

		/*
		 * Attempt to locate the user using the credentials. If not found, terminate silently.
		 */
		var user = await users.Select(dto);

		if (user is null)
			return;

		/*
		 * Resolve an identity update DTO, assign the authenticated user and propagate
		 * the new identity through the authentication service.
		 */
		var identityDto = scope.ServiceProvider.GetRequiredService<IUpdateIdentityDto>();

		identityDto.Identity = user;

		await authentication.UpdateIdentity(identityDto);

		/*
		 * Flush the scope to ensure any buffered identity or side-effect operations are committed.
		 */
		await scope.Flush();
	}
}