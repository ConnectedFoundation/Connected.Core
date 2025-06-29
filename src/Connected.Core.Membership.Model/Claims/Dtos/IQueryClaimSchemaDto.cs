using Connected.Services;

namespace Connected.Membership.Claims.Dtos;

public interface IQueryClaimSchemaDto : IDto
{
	string? Id { get; set; }
}
