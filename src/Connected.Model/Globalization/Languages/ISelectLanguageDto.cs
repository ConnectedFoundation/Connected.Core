using Connected.Services;

namespace Connected.Globalization.Languages;
public interface ISelectLanguageDto : IDto
{
	int Lcid { get; set; }
}
