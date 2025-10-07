using Connected.Core.Services.Mock;
using Connected.Membership.Claims.Dtos;

namespace Connected.Core.Membership.Mock.Claims.Dtos;
public class ClaimDtoMock
	: DtoMock, IClaimDto
{
	public required string Value { get; set; }
	public string? Schema { get; set; }
	public string? Identity { get; set; }
	public required string Entity { get; set; }
	public required string EntityId { get; set; }
}
