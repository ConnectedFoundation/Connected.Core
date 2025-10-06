using Connected.Core.Entities.Mock;
using Connected.Membership.Claims;

namespace Connected.Core.Membership.Mock.Claims;
public class ClaimMock : EntityMock<long>, IClaim
{
	public required string Value { get; init; }
	public string? Schema { get; init; }
	public string? Identity { get; init; }
	public required string Entity { get; init; }
	public required string EntityId { get; init; }
	public ClaimStatus Status { get; init; }
}
