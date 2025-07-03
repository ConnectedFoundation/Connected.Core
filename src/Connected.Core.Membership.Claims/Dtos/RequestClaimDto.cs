using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Membership.Claims.Dtos;

internal sealed class RequestClaimDto : Dto, IRequestClaimDto
{
	[Required]
	public required string Values { get; set; }

	[MaxLength(256)]
	public string? Identity { get; set; }

	[MaxLength(256)]
	public string? Type { get; set; }

	[MaxLength(256)]
	public string? PrimaryKey { get; set; }
}
