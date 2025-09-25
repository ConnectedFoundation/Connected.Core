using Connected.Services;

namespace Connected.Membership.Claims;

public interface IQueryClaimDto : IDto
{
	string? Schema { get; set; }
	string? Identity { get; set; }
	string Entity { get; set; }
	string EntityId { get; set; }
}