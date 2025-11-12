using Connected.Services;

namespace Connected.Membership.Claims.Dtos;

public interface IRequestClaimDto : IDto
{
	string Values { get; set; }
	string? Identity { get; set; }
	string Entity { get; set; }
	string EntityId { get; set; }
}