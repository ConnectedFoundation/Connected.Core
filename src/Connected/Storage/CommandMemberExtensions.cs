using Connected.Annotations.Entities;
using Connected.Reflection;
using System.Reflection;

namespace Connected.Storage;

/// <summary>
/// Decides, once for every provider, which members of an entity take part in the commands the providers build.
/// </summary>
public static class CommandMemberExtensions
{
	/// <summary>
	/// Whether a member is part of the commands built for its entity.
	/// </summary>
	/// <param name="property">The member of the entity.</param>
	/// <returns><c>true</c> for a member without a <see cref="PersistenceAttribute"/> and for the version (<see cref="ETagAttribute"/>) that is
	/// read from the database (<see cref="PersistenceMode.Read"/>); <c>false</c> for every other member that carries one.</returns>
	/// <remarks>
	/// A member that carries a persistence attribute is kept out of the commands, as it always was. The version is the exception: it is
	/// read only (<see cref="PersistenceMode.Read"/>) and the provider keeps it, but an update names it in its WHERE so that a change made
	/// by someone else since the entity was read is refused. The providers used to ask
	/// <c>Mode.HasFlag(PersistenceMode.InMemory)</c>, which is true for every mode because <c>InMemory</c> is 0, and so left the version out.
	/// </remarks>
	public static bool IsCommandMember(this PropertyInfo property)
	{
		var persistence = property.FindAttribute<PersistenceAttribute>();

		return persistence is null || (property.FindAttribute<ETagAttribute>() is not null && persistence.Mode.HasFlag(PersistenceMode.Read));
	}
}
