using Connected.Configuration.Authentication;

namespace Connected.Identities;
/// <summary>
/// Token lifetime configuration with no idle timeout and no maximum lifetime.
/// </summary>
/// <remarks>
/// Used by callers that do not supply a configuration, so that expiring tokens keep the framework defaults.
/// </remarks>
internal sealed class DefaultTokenConfiguration
	: IAuthenticationTokenConfiguration
{
	/// <summary>
	/// Gets the shared instance.
	/// </summary>
	public static DefaultTokenConfiguration Instance { get; } = new();

	/// <inheritdoc/>
	public int? IdleTimeout => null;

	/// <inheritdoc/>
	public int? MaxLifetime => null;
}
