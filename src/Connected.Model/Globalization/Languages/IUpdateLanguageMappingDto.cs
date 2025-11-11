using Connected.Services;

namespace Connected.Globalization.Languages;
/// <summary>
/// DTO for updating an existing language mapping.
/// </summary>
public interface IUpdateLanguageMappingDto
	: IDto
{
	/// <summary>
	/// Gets or sets the primary key of the language mapping to update.
	/// </summary>
	int Id { get; set; }
	/// <summary>
	/// Gets or sets the language identifier (primary key of the associated language).
	/// </summary>
	int Language { get; set; }
	/// <summary>
	/// Gets or sets the external mapping value or key.
	/// </summary>
	string Mapping { get; set; }
}
