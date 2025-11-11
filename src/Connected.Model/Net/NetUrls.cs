namespace Connected.Net;

/// <summary>
/// Provides URL path constants for networking service endpoints.
/// </summary>
/// <remarks>
/// This static class centralizes URL path definitions for network-related services
/// including routing, messaging, and event endpoints. Maintaining these constants
/// in a single location ensures consistency across the platform and simplifies URL
/// management when endpoints need to be updated or refactored. The URL patterns
/// follow a hierarchical namespace structure for logical organization of networking
/// services.
/// </remarks>
public static class NetUrls
{
	/// <summary>
	/// Gets the base namespace path for networking services.
	/// </summary>
	/// <value>
	/// The string "services/net" representing the root path for networking service endpoints.
	/// </value>
	/// <remarks>
	/// This constant serves as the base path for all networking-related service endpoints,
	/// providing a consistent prefix that helps organize and identify network service URLs.
	/// </remarks>
	private const string Namespace = "services/net";

	/*
	 * Commented endpoint: Routing service path.
	 * When enabled, this would provide the URL path for the routing service API.
	 * Pattern: "{Namespace}/routing" which would resolve to "services/net/routing".
	 */
	//public const string Routing = $"{Namespace}/routing";
}