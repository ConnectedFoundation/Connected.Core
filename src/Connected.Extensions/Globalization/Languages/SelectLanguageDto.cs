using Connected.Annotations;
using Connected.Globalization.Languages.Dtos;
using Connected.Services;

namespace Connected.Globalization.Languages;
internal class SelectLanguageDto : Dto, ISelectLanguageDto
{
	[MinValue(1)]
	public int Lcid { get; set; }
}
