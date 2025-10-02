using Connected.Entities;

namespace Connected.Identities;
public interface IIdentityDescriptor : IEntity, IIdentity
{
	string Name { get; init; }
}
