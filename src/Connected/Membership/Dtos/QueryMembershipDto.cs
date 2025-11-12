using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Membership.Dtos;

internal sealed class QueryMembershipDto : Dto, IQueryMembershipDto
{
	[MaxLength(256)]
	public string? Identity { get; set; }
	public int? Role { get; set; }
}
