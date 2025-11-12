using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Membership.Roles;

[Table(Schema = SchemaAttribute.CoreSchema)]
internal sealed record Role : ConsistentEntity<int>, IRole
{
	[Ordinal(0), Length(128), Index(true)]
	public required string Name { get; init; }

	[Ordinal(1)]
	public Status Status { get; init; }

	[Ordinal(2)]
	public int? Parent { get; init; }

	[Ordinal(3), Length(128), Index(true)]
	public required string Token { get; init; }
}
