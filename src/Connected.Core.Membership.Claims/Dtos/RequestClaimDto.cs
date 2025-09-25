using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Membership.Claims.Dtos;

internal sealed class RequestClaimDto : Dto, IRequestClaimDto
{
	[Required]
	public required string Values { get; set; }

	[MaxLength(256)]
	public string? Identity { get; set; }

	[Required, MaxLength(256)]
	public required string Entity { get; set; }

	[Required, MaxLength(256)]
	public required string EntityId { get; set; }
}
