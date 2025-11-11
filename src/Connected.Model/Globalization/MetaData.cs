using Connected.Globalization.Languages;

namespace Connected.Globalization;

/// <summary>
/// Provides metadata constants for the globalization module.
/// </summary>
/// <remarks>
/// This class contains schema definitions and entity key identifiers used throughout
/// the globalization subsystem for consistent naming and reference resolution.
/// </remarks>
public static class MetaData
{
	/// <summary>
	/// The database schema name for globalization-related entities.
	/// </summary>
	public const string Schema = "globalization";

	/// <summary>
	/// The fully qualified key identifier for the Language entity.
	/// </summary>
	public const string LanguageKey = $"{Schema}.{nameof(ILanguage)}";

	/// <summary>
	/// The fully qualified key identifier for the LanguageMapping entity.
	/// </summary>
	public const string LanguageMappingKey = $"{Schema}.{nameof(ILanguageMapping)}";
}