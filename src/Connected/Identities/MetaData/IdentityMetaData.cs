using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;
using System.Text.Json.Serialization;

namespace Connected.Identities.MetaData;

[Table(Schema = SchemaAttribute.CoreSchema)]
internal sealed record IdentityMetaData : Entity, IIdentityMetaData
{
	[Ordinal(0), Length(256), PrimaryKey(false)]
	public required string Id { get; init; }

	[Ordinal(1), Length(1024)]
	public string? Url { get; init; }

	[Ordinal(2), Length(1024)]
	public string? Description { get; init; }

	[Ordinal(3), Length(1024)]
	public string? Avatar { get; init; }

	[Ordinal(4), Length(128)]
	public string? UserName { get; init; }

	[Ordinal(10000), ETag, JsonIgnore]
	public string? ETag { get; init; }

}
