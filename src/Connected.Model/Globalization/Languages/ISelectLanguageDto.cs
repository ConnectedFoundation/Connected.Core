using Connected.Services;

namespace Connected.Globalization.Languages;
/// <summary>
/// DTO for selecting a language by its locale identifier (LCID).
/// </summary>
public interface ISelectLanguageDto
	: IDto
{
	/// <summary>
	/// Gets or sets the locale identifier (LCID) to select.
	/// </summary>
	int Lcid { get; set; }
}
