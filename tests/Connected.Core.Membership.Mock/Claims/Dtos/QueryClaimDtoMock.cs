using Connected.Core.Services.Mock;
using Connected.Membership.Claims;

namespace Connected.Core.Membership.Mock.Claims.Dtos;
public class QueryClaimDtoMock
	: DtoMock, IQueryClaimDto
{
	public string? Schema { get; set; }
	public string? Identity { get; set; }
	public required string Entity { get; set; }
	public required string EntityId { get; set; }
}
