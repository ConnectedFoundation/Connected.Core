namespace Connected.Annotations.Entities;
/// <summary>
/// Defines an Entity member name on the property when storage naming differs from the CLR property name.
/// Common when mapping to pre-existing schemas where names cannot be changed.
/// </summary>
public class MemberAttribute
	: MappingAttribute
{
	/*
	 * Holds an alternate storage member name. If null, consumers fall back to the CLR property name
	 * for schema generation and mapping operations.
	 */
	/// <summary>
	/// The name of the property on the storage. If this value is null, the property name is used.
	/// </summary>
	public string? Member { get; set; }
}
