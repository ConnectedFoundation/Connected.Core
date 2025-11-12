using Connected.Annotations;
using System.Security.Principal;
using System.Text;

namespace Connected.Authentication;
/// <summary>
/// Base implementation of Basic authentication. Parses the Basic Authorization header
/// and exposes the parsed credentials to derived providers which implement the actual
/// authentication logic.
/// </summary>
/// <remarks>
/// This implementation expects that a Basic scheme has been set on the DTO (schema "basic").
/// </remarks>
[Priority(1)]
[ServiceRegistration(ServiceRegistrationMode.Manual)]
public abstract class BasicAuthenticationProvider
	: AuthenticationProvider
{
	/// <summary>
	/// Gets the parsed user name extracted from the authentication DTO, if available.
	/// </summary>
	protected string? UserName { get; private set; }
	/// <summary>
	/// Gets the parsed password extracted from the authentication DTO, if available.
	/// </summary>
	protected string? Password { get; private set; }
	/// <summary>
	/// Entry point invoked after <see cref="AuthenticationProvider.Invoke(IAuthenticateDto)"/> captures
	/// the DTO. Parses credentials and, if present, delegates to <see cref="OnAuthenticate"/>.
	/// </summary>
	/// <returns>A task that completes when processing is finished.</returns>
	protected override async Task OnInvoke()
	{
		/*
		 * Parse Basic credentials from the DTO (schema and token). If extraction fails
		 * or credentials are missing, return without altering identity; otherwise let
		 * derived classes authenticate the provided user name and password.
		 */
		ParseUserNameAndPassword();
		if (UserName is null || Password is null)
			return;
		await OnAuthenticate();
	}
	/// <summary>
	/// Parses the <see cref="UserName"/> and <see cref="Password"/> from the current DTO when the
	/// schema is "basic" and the token contains a Base64-encoded "user:password" string.
	/// </summary>
	private void ParseUserNameAndPassword()
	{
		/*
		 * Validate that the DTO indicates Basic authentication; skip parsing for other schemes.
		 */
		if (!string.Equals(Dto.Schema, "basic", StringComparison.OrdinalIgnoreCase))
			return;
		/*
		 * Ensure we have a non-empty token before attempting to decode.
		 */
		if (string.IsNullOrWhiteSpace(Dto.Token))
			return;
		/*
		 * Decode the Base64 token using UTF-8 to retrieve the credential payload in the
		 * canonical "username:password" format, then split at the first colon only.
		 */
		var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(Dto.Token));
		var credentialTokens = credentials.Split([':'], 2);
		/*
		 * Assign the username token; if a password segment exists, assign it as well.
		 */
		UserName = credentialTokens[0];
		if (credentialTokens.Length > 1)
			Password = credentialTokens[1];
	}
	/// <summary>
	/// Called when <see cref="UserName"/> and <see cref="Password"/> are available. Implementations
	/// should perform authentication against the provided credentials and update identity as needed.
	/// </summary>
	/// <returns>A task representing the asynchronous authentication operation.</returns>
	protected virtual Task OnAuthenticate()
	{
		/*
		 * Default implementation performs no authentication. Override in derived classes
		 * to validate credentials and update identity state.
		 */
		return Task.FromResult<IIdentity?>(null);
	}
}