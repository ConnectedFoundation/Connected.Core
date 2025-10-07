using Connected.Globalization.Languages;

namespace Connected.Globalization;
public static class MetaData
{
	public const string Schema = "globalization";

	public const string LanguageKey = $"{Schema}.{nameof(ILanguage)}";
	public const string LanguageMappingKey = $"{Schema}.{nameof(ILanguageMapping)}";
}