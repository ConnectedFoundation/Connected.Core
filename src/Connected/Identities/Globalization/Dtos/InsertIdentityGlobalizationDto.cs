using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities.Globalization.Dtos;
internal sealed class InsertIdentityGlobalizationDto : Dto, IInsertIdentityGlobalizationDto
{
	[Required, MaxLength(128)]
	public required string Id { get; set; }

	[MaxLength(256)]
	public string? TimeZone { get; set; }

	public int? Language { get; set; }
}
