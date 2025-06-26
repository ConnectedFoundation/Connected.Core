using Connected.Annotations;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Membership.Dtos;

internal sealed class InsertMembershipDto : Dto, IInsertMembershipDto
{
	[Required, MaxLength(256)]
	public required string Identity { get; set; }

	[MinValue(1)]
	public int Role { get; set; }
}
