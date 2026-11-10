namespace Connected.Configuration.Authentication;
/// <summary>
/// Configuration contract describing the lifetime of expiring authentication tokens.
/// </summary>
/// <remarks>
/// Both values are optional. When neither is set, expiring tokens keep the framework defaults: a token
/// issued at sign-in expires after 30 minutes and each ping or reset extends it by 20 minutes, without
/// an upper bound. Permanent tokens are never affected by these values.
/// </remarks>
public interface IAuthenticationTokenConfiguration
{
	/// <summary>
	/// Gets the number of minutes of inactivity after which an expiring token lapses.
	/// </summary>
	/// <remarks>
	/// When set, the value replaces both the sign-in and the ping or reset durations.
	/// </remarks>
	int? IdleTimeout { get; }
	/// <summary>
	/// Gets the maximum number of minutes an expiring token may stay valid after it was created.
	/// </summary>
	/// <remarks>
	/// When set, no sign-in, ping or reset extends a token beyond its creation time plus this value.
	/// </remarks>
	int? MaxLifetime { get; }
}
