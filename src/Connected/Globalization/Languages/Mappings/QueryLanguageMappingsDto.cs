using Connected.Services;

namespace Connected.Globalization.Languages.Mappings;
internal sealed class QueryLanguageMappingsDto : Dto, IQueryLanguageMappingsDto
{
	public int? Language { get; set; }
}
