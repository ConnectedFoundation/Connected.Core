namespace Connected.Configuration.Authentication;
/// <summary>
/// Root configuration contract for authentication-related settings.
/// </summary>
/// <remarks>
/// Acts as a composition root for authentication options, grouping JWT and other future
/// settings under a single configuration interface consumed by services.
/// </remarks>
public interface IAuthenticationConfiguration
{
	/// <summary>
	/// Gets the JWT token configuration.
	/// </summary>
	IJwTokenConfiguration JwToken { get; }
}
