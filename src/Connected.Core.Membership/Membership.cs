using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Membership;

internal sealed record Membership : Entity<long>, IMembership
{
	[Ordinal(0), Length(256)]
	public required string Identity { get; init; }

	[Ordinal(1)]
	public int Role { get; init; }
}
