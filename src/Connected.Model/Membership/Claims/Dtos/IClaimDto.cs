using Connected.Services;

namespace Connected.Membership.Claims.Dtos;

public interface IClaimDto : IDto
{
	string Value { get; set; }
	string? Schema { get; set; }
	string? Identity { get; set; }
	string Entity { get; set; }
	string EntityId { get; set; }
}
