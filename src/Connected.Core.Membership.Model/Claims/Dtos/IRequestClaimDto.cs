using Connected.Services;

namespace Connected.Membership.Claims.Dtos;

public interface IRequestClaimDto : IDto
{
	string Values { get; set; }
	string? Identity { get; set; }
	string? Type { get; set; }
	string? PrimaryKey { get; set; }
}