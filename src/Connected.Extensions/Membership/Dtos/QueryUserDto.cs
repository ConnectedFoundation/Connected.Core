using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Membership.Dtos;

internal sealed class QueryUserDto
	: Dto, IQueryUserDto
{
	[Required, MaxLength(DefaultIdentityLength)]
	public required string Role { get; set; }
}
