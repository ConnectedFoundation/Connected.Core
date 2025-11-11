using Connected.Entities;
using Connected.Services;

namespace Connected.Globalization.Languages;
/// <summary>
/// DTO for updating an existing language record.
/// </summary>
public interface IUpdateLanguageDto
	: IDto
{
	/// <summary>
	/// Gets or sets the primary key of the language to update.
	/// </summary>
	int Id { get; set; }
	/// <summary>
	/// Gets or sets the display name of the language.
	/// </summary>
	string Name { get; set; }
	/// <summary>
	/// Gets or sets the availability status of the language.
	/// </summary>
	Status Status { get; set; }
	/// <summary>
	/// Gets or sets the locale identifier (LCID) for the language.
	/// </summary>
	int Lcid { get; set; }
	/// <summary>
	/// Gets or sets the culture identifier (e.g., "en-US", "de-DE").
	/// </summary>
	string Culture { get; set; }
}
