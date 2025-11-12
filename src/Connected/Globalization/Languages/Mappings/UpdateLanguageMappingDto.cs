using Connected.Annotations;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Globalization.Languages.Mappings;
internal sealed class UpdateLanguageMappingDto : Dto, IUpdateLanguageMappingDto
{
	[MinValue(1)]
	public int Id { get; set; }

	[MinValue(1)]
	public int Language { get; set; }

	[Required, MaxLength(32)]
	public required string Mapping { get; set; }
}
