namespace Connected.Globalization;

/// <summary>
/// Provides URL constants for globalization service endpoints.
/// </summary>
/// <remarks>
/// This class centralizes all service endpoint paths for the globalization module,
/// ensuring consistent routing across the application for language and language mapping services.
/// </remarks>
public static class Urls
{
	/// <summary>
	/// The base namespace path for globalization services.
	/// </summary>
	private const string Namespace = "services/globalization";

	/// <summary>
	/// The service endpoint URL for language operations.
	/// </summary>
	public const string LanguageService = $"{Namespace}/languages";

	/// <summary>
	/// The service endpoint URL for language mapping operations.
	/// </summary>
	public const string LanguageMappingService = $"{Namespace}/languages/mappings";
}
