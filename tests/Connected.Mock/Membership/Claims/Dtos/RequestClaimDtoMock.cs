using Connected.Core.Services.Mock;
using Connected.Membership.Claims.Dtos;

namespace Connected.Core.Membership.Mock.Claims.Dtos;
public class RequestClaimDtoMock
	: DtoMock, IRequestClaimDto
{
	public required string Values { get; set; }
	public string? Identity { get; set; }
	public required string Entity { get; set; }
	public required string EntityId { get; set; }
}
