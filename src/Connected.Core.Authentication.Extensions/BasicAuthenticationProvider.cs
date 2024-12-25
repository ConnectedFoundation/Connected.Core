using Connected.Annotations;
using System.Security.Principal;
using System.Text;

namespace Connected.Authentication;
/// <summary>
/// Base implementation of the basic authentication.
/// </summary>
/// <remarks>
/// This implementation expects that a basic scheme has been set to a Dto.
/// </remarks>
[Priority(1)]
public abstract class BasicAuthenticationProvider : AuthenticationProvider
{
	/// <summary>
	/// The parsed user name from the Dto.
	/// </summary>
	protected string? UserName { get; private set; }
	/// <summary>
	/// The parsed password from the Dto.
	/// </summary>
	protected string? Password { get; private set; }
	/// <inheritdoc cref="AuthenticationProvider.OnInvoke"/>>
	protected override async Task OnInvoke()
	{
		ParseUserNameAndPassword();

		if (UserName is null || Password is null)
			return;

		await OnAuthenticate();
	}

	private void ParseUserNameAndPassword()
	{
		if (!string.Equals(Dto.Schema, "basic", StringComparison.OrdinalIgnoreCase))
			return;

		if (string.IsNullOrWhiteSpace(Dto.Token))
			return;

		var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(Dto.Token));
		var credentialTokens = credentials.Split(new char[] { ':' }, 2);

		UserName = credentialTokens[0];

		if (credentialTokens.Length > 1)
			Password = credentialTokens[1];
	}
	/// <summary>
	/// Implementations of the basic authentication should override this method to perform
	/// authentication against user name and password.
	/// </summary>
	protected virtual Task OnAuthenticate()
	{
		return Task.FromResult<IIdentity?>(null);
	}
}