using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;
using System.Text.Json.Serialization;

namespace Connected.Identities.Users;

[Table(Schema = SchemaAttribute.CoreSchema)]
internal sealed record User : ConsistentEntity<long>, IUser
{
	[Ordinal(0), Length(64)]
	public string? FirstName { get; init; }

	[Ordinal(1), Length(64)]
	public string? LastName { get; init; }

	[Ordinal(2), Length(1024)]
	public string? Email { get; init; }

	[Ordinal(3)]
	public UserStatus Status { get; init; }

	[Ordinal(5), Length(256), JsonIgnore]
	public string? Password { get; init; }

	[Ordinal(6), Length(128)]
	public required string Token { get; init; }
}
