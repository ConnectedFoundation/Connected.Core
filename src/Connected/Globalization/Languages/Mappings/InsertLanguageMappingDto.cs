using Connected.Annotations;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Globalization.Languages.Mappings;
internal sealed class InsertLanguageMappingDto : Dto, IInsertLanguageMappingDto
{
	[MinValue(1)]
	public int Language { get; set; }

	[Required, MaxLength(32)]
	public required string Mapping { get; set; }
}
