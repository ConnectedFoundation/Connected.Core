using Connected.Entities;
using Connected.Services;

namespace Connected.Globalization.Languages;
/// <summary>
/// DTO for creating a new language record in the system.
/// </summary>
public interface IInsertLanguageDto
	: IDto
{
	/// <summary>
	/// Gets or sets the display name of the language.
	/// </summary>
	string Name { get; set; }
	/// <summary>
	/// Gets or sets the culture identifier (e.g., "en-US", "de-DE").
	/// </summary>
	string Culture { get; set; }
	/// <summary>
	/// Gets or sets the availability status of the language.
	/// </summary>
	Status Status { get; set; }
	/// <summary>
	/// Gets or sets the locale identifier (LCID) for the language.
	/// </summary>
	int Lcid { get; set; }
}
