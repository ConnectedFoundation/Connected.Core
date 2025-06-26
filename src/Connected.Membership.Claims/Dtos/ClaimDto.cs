using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Membership.Claims.Dtos;

internal abstract class ClaimDto : Dto, IClaimDto
{
	[Required, MaxLength(256)]
	public required string Value { get; set; }

	[MaxLength(256)]
	public string? Schema { get; set; }

	[MaxLength(256)]
	public string? Identity { get; set; }

	[MaxLength(256)]
	public string? Type { get; set; }

	[MaxLength(256)]
	public string? PrimaryKey { get; set; }
}
