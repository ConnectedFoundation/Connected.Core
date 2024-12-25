using Connected.Services;

namespace Connected.Globalization.Languages;
public interface IInsertLanguageMappingDto : IDto
{
	int Language { get; set; }
	string Mapping { get; set; }
}
