using Connected.Annotations;
using Connected.Entities;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Globalization.Languages;
internal sealed class InsertLanguageDto : Dto, IInsertLanguageDto
{
	[Required, MaxLength(32)]
	public required string Name { get; set; }

	public Status Status { get; set; } = Status.Disabled;

	[MinValue(1)]
	public int Lcid { get; set; }

	[Required, MaxLength(32)]
	public required string Culture { get; set; }
}
