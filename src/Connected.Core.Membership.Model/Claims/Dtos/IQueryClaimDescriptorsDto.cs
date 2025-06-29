using Connected.Services;

namespace Connected.Membership.Claims.Dtos;

public interface IQueryClaimDescriptorsDto : IDto
{
	string Id { get; set; }
	string Type { get; set; }
}
