using Connected.Entities;

namespace Connected.Identities;
public record IdentityDescriptor : Entity, IIdentityDescriptor
{
	public required string Name { get; init; }
	public required string Token { get; init; }
}

