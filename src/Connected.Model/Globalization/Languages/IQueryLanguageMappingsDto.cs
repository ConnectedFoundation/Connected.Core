using Connected.Services;

namespace Connected.Globalization.Languages;
public interface IQueryLanguageMappingsDto : IDto
{
	int? Language { get; set; }
}
