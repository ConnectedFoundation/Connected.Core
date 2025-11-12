using Connected.Annotations;
using Connected.Services;

namespace Connected.Globalization.Languages;
internal sealed class SelectLanguageDto : Dto, ISelectLanguageDto
{
	[MinValue(1)]
	public int Lcid { get; set; }
}
