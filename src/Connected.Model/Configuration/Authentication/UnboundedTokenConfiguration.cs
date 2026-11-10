namespace Connected.Configuration.Authentication;
/// <summary>
/// Token lifetime configuration with no idle timeout and no maximum lifetime.
/// </summary>
/// <remarks>
/// Backs the default implementation of <see cref="IAuthenticationConfiguration.Tokens"/>.
/// </remarks>
internal sealed class UnboundedTokenConfiguration
	: IAuthenticationTokenConfiguration
{
	/// <summary>
	/// Gets the shared instance.
	/// </summary>
	public static UnboundedTokenConfiguration Instance { get; } = new();

	/// <inheritdoc/>
	public int? IdleTimeout => null;

	/// <inheritdoc/>
	public int? MaxLifetime => null;
}
