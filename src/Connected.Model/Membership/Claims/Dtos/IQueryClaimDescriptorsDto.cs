using Connected.Services;

namespace Connected.Membership.Claims.Dtos;

public interface IQueryClaimDescriptorsDto : IDto
{
	string EntityId { get; set; }
	string Entity { get; set; }
}
