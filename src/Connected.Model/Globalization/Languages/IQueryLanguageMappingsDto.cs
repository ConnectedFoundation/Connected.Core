using Connected.Services;

namespace Connected.Globalization.Languages;
/// <summary>
/// DTO for querying language mappings with optional filtering by language identifier.
/// </summary>
public interface IQueryLanguageMappingsDto
	: IDto
{
	/// <summary>
	/// Gets or sets the language identifier to filter mappings; null returns all mappings.
	/// </summary>
	int? Language { get; set; }
}
