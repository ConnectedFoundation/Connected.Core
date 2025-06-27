using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Identities.Globalization;

[Table(Schema = SchemaAttribute.CoreSchema)]
internal sealed record IdentityGlobalization : ConsistentEntity<string>, IIdentityGlobalization
{
	[Ordinal(0), Length(128), PrimaryKey(false)]
	public required override string Id { get; init; }

	[Ordinal(1), Length(1024)]
	public string? TimeZone { get; init; }

	[Ordinal(2)]
	public int? Language { get; init; }
}