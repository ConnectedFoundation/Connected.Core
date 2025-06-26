using Connected.Services;

namespace Connected.Membership.Claims;

public interface IClaimDto : IDto
{
	string Value { get; set; }
	string? Schema { get; set; }
	string? Identity { get; set; }
	string? Type { get; set; }
	string? PrimaryKey { get; set; }
}
