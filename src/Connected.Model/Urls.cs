namespace Connected;

/// <summary>
/// Provides URL path constants for core application endpoints.
/// </summary>
/// <remarks>
/// This static class centralizes URL path definitions used throughout the application
/// for routing and navigation. Maintaining these constants in a single location ensures
/// consistency across the platform and simplifies URL management when endpoints need to
/// be updated or refactored.
/// </remarks>
public static class Urls
{
	/// <summary>
	/// Gets the URL path for the configuration settings endpoint.
	/// </summary>
	/// <value>
	/// The string "/configuration/settings" representing the settings configuration route.
	/// </value>
	/// <remarks>
	/// This path is used to access the application's configuration settings interface,
	/// typically for viewing or modifying system-wide configuration parameters.
	/// </remarks>
	public const string Settings = "/configuration/settings";
}