using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Identities.Authentication;

[Table(Schema = SchemaAttribute.CoreSchema)]

internal sealed record IdentityAuthenticationToken : ConsistentEntity<long>, IIdentityAuthenticationToken
{
	[Ordinal(0), Length(256)]
	public required string Key { get; init; }

	[Ordinal(1), Length(256)]
	public string? Token { get; init; }

	[Ordinal(2), Length(256)]
	public required string Identity { get; init; }

	[Ordinal(3)]
	public AuthenticationTokenStatus Status { get; init; }

	[Ordinal(4), Date(DateKind.DateTime2, 7)]
	public DateTimeOffset? Expire { get; init; }
}
