using Connected.Entities;

namespace Connected.Membership.Claims;

public enum ClaimStatus
{
	Pending = 1,
	Approved = 2,
	Denied = 3
}

public interface IClaim : IEntity<long>
{
	string Value { get; init; }
	string? Schema { get; init; }
	string? Identity { get; init; }
	string Entity { get; init; }
	string EntityId { get; init; }
	ClaimStatus Status { get; init; }
}