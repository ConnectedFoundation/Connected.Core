using Connected.Identities;
using Connected.Identities.Authentication;
using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Authentication;
/// <summary>
/// Authentication provider that validates bearer tokens stored in the identity token service.
/// Upon successful validation the user identity is loaded and applied to the ambient context and
/// the current HTTP request principal.
/// </summary>
/// <param name="http">Accessor used to update the current <see cref="HttpContext"/> principal when authentication succeeds.</param>
internal sealed class TokenAuthentication(IHttpContextAccessor http)
	: BearerAuthenticationProvider
{
	/// <summary>
	/// Performs token-based authentication workflow: validates token presence, resolves services,
	/// loads token record, checks expiry and status, loads the associated identity and applies it.
	/// </summary>
	/// <returns>A task that completes when authentication processing finishes.</returns>
	protected override async Task OnAuthenticate()
	{
		/*
		 * Ensure a bearer token was parsed by the base provider. If absent, nothing to authenticate.
		 */
		if (Token is null)
			return;
		/*
		 * Create a system-elevated scope so privileged services (token store, identity extensions,
		 * authentication) can be accessed securely.
		 */
		using var scope = await Scope.Create().WithSystemIdentity();

		/*
		 * Resolve required services for authentication: the service that applies identity updates,
		 * the token service for retrieving and validating authentication tokens, and extensions for
		 * loading domain identities.
		 */
		var authentication = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

		var tokens = scope.ServiceProvider.GetRequiredService<IIdentityAuthenticationTokenService>();
		/*
		 * Query the token record using the raw token string. If not found, abort early.
		 */
		var token = await tokens.Select(Dto.CreateValue(Token));

		if (token is null)
			return;
		/*
		 * Expiration check: if the token is expired, remove it from the store to prevent reuse then exit.
		 */
		if (token.Expire < DateTimeOffset.UtcNow)
		{
			await tokens.Delete(Dto.CreatePrimaryKey(token.Id));

			return;
		}
		/*
		 * Status check: disabled tokens are ignored without applying identity.
		 */
		if (token.Status == AuthenticationTokenStatus.Disabled)
			return;
		/*
		 * Prepare DTOs for identity resolution and update. Load the identity reference using extensions
		 * and assign it to the update DTO.
		 */
		var identityDto = new Dto<IUpdateIdentityDto>().Value;
		var extensions = scope.ServiceProvider.GetRequiredService<IIdentityExtensions>();
		var dto = new Dto<IValueDto<string>>().Value;

		dto.Value = token.Identity;

		identityDto.Identity = await extensions.Select(dto);
		/*
		 * If the identity was successfully loaded, update the ambient identity and set the current
		 * HTTP principal to reflect authenticated status.
		 */
		if (identityDto.Identity is not null)
		{
			await authentication.UpdateIdentity(identityDto);

			/*
			 * Assign a ClaimsPrincipal bridging to the domain identity when an HTTP context exists.
			 */
			if (http.HttpContext is not null)
			{
				http.HttpContext.User = new DefaultPrincipal(new HttpIdentity(identityDto.Identity)
				{
					IsAuthenticated = true
				});
			}
		}
		/*
		 * Flush scope to persist any buffered operations (e.g., token deletion, identity updates).
		 */
		await scope.Flush();
	}
}
