using Connected.Services;

namespace Connected.Membership.Claims.Dtos;

public interface IQueryClaimDto : IDto
{
	List<string> Schemas { get; set; }
	List<string> Identities { get; set; }
	List<string> Entities { get; set; }
	List<string> EntityIds { get; set; }
	List<string> Values { get; set; }
	List<ClaimStatus> Statuses { get; set; }
}