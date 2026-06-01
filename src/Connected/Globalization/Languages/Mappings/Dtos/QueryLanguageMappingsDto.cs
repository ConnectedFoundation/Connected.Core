using Connected.Services;

namespace Connected.Globalization.Languages.Mappings.Dtos;
internal sealed class QueryLanguageMappingsDto : Dto, IQueryLanguageMappingDto
{
	public int? Language { get; set; }
}
