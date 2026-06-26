using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Membership.Claims;

public enum ClaimStatus
{
	Pending = 1,
	Approved = 2,
	Denied = 3
}

[EntityKey(MembershipMetaData.ClaimKey)]
public interface IClaim : IEntity<long>
{
	const string UndefinedEntity = "*";
	const string UndefinedId = "*";

	string Value { get; init; }
	string? Schema { get; init; }
	string? Identity { get; init; }
	string Entity { get; init; }
	string EntityId { get; init; }
	ClaimStatus Status { get; init; }
}