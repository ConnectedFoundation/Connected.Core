using Connected.Services;

namespace Connected.Membership.Claims;

public interface IRequestClaimDto : IDto
{
	string Claims { get; set; }
	string? Identity { get; set; }
	string? Type { get; set; }
	string? PrimaryKey { get; set; }
}