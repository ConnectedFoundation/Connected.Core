using Connected.Entities;

namespace Connected.Identities;

/// <summary>
/// Represents a descriptor for an identity that combines entity and identity characteristics.
/// </summary>
/// <remarks>
/// This interface extends both entity and identity contracts to provide a comprehensive
/// descriptor that includes an identifier, security token, and a human-readable name.
/// It serves as a lightweight representation of an identity for display and reference purposes.
/// </remarks>
public interface IIdentityDescriptor
	: IEntity, IIdentity
{
	/// <summary>
	/// Gets the human-readable name of the identity.
	/// </summary>
	string Name { get; init; }
}
