using Connected.Annotations;
using System.Text;

namespace Connected.Authentication;
/// <summary>
/// Base implementation of the bearer authentication.
/// </summary>
/// <remarks>
/// This implementation expects that a bearer scheme has been set to a Dto.
/// </remarks>
[Priority(0)]
public abstract class BearerAuthenticationProvider : AuthenticationProvider
{
	/// <summary>
	/// The parsed bearer token from the Dto.
	/// </summary>
	protected string? Token { get; private set; }
	/// <inheritdoc cref="AuthenticationProvider.OnInvoke"/>>
	protected override async Task OnInvoke()
	{
		ParseToken();

		if (Token is null)
			return;

		await OnAuthenticate();
	}
	/// <summary>
	/// Implementations of the bearer authentication should override this method to perform
	/// authentication against the token.
	/// </summary>
	protected virtual Task OnAuthenticate()
	{
		return Task.CompletedTask;
	}

	private void ParseToken()
	{
		if (!string.Equals(Dto.Schema, "bearer", StringComparison.OrdinalIgnoreCase))
			return;

		if (string.IsNullOrWhiteSpace(Dto.Token))
			return;


		try
		{
			Token = Encoding.UTF8.GetString(Convert.FromBase64String(Dto.Token));
		}
		catch
		{
		}
	}
}