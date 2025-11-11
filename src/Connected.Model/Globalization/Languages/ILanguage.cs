using Connected.Entities;

namespace Connected.Globalization.Languages;
/// <summary>
/// Represents a language configuration entity with culture, locale, and availability information.
/// </summary>
public interface ILanguage
	: IEntity<int>
{
	/// <summary>
	/// Gets the display name of the language.
	/// </summary>
	string Name { get; init; }
	/// <summary>
	/// Gets the locale identifier (LCID) for the language.
	/// </summary>
	int Lcid { get; init; }
	/// <summary>
	/// Gets the availability status of the language.
	/// </summary>
	Status Status { get; init; }
	/// <summary>
	/// Gets the culture identifier (e.g., "en-US", "de-DE").
	/// </summary>
	string Culture { get; init; }
}
