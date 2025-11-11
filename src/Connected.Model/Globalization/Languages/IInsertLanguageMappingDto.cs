using Connected.Services;

namespace Connected.Globalization.Languages;
/// <summary>
/// DTO for creating a new language mapping that associates a language with an external identifier or key.
/// </summary>
public interface IInsertLanguageMappingDto
	: IDto
{
	/// <summary>
	/// Gets or sets the language identifier (primary key).
	/// </summary>
	int Language { get; set; }
	/// <summary>
	/// Gets or sets the external mapping value or key associated with the language.
	/// </summary>
	string Mapping { get; set; }
}
