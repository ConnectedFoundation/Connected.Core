using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Membership.Claims.Dtos;

internal sealed class QueryClaimSchemaDto : Dto, IQueryClaimSchemaDto
{
	[MaxLength(256)]
	public string? Id { get; set; }
}
