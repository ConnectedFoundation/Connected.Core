using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Membership.Claims.Dtos;

internal sealed class QueryClaimDto : Dto, IQueryClaimDto
{
	public List<string> Schemas { get; set; } = [];
	public List<string> Identities { get; set; } = [];
	public List<string> Entities { get; set; } = [];
	public List<string> EntityIds { get; set; } = [];
	public List<string> Values { get; set; } = [];
	public List<ClaimStatus> Statuses { get; set; } = [];
}