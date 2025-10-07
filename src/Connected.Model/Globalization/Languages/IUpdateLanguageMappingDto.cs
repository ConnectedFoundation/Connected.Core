using Connected.Services;

namespace Connected.Globalization.Languages;
public interface IUpdateLanguageMappingDto : IDto
{
	int Id { get; set; }
	int Language { get; set; }
	string Mapping { get; set; }
}
