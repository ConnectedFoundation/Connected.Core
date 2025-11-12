using Connected.Services;

namespace Connected.Membership.Claims.Dtos;

public interface IQueryClaimSchemaDto : IDto
{
	string? Entity { get; set; }
	string? EntityId { get; set; }
}
