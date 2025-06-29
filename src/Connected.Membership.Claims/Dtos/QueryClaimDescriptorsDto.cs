using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Membership.Claims.Dtos;

internal sealed class QueryClaimDescriptorsDto : Dto, IQueryClaimDescriptorsDto
{
	[Required, MaxLength(256)]
	public required string Id { get; set; }

	[Required, MaxLength(256)]
	public required string Type { get; set; }
}
