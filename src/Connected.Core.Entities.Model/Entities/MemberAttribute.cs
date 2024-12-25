namespace Connected.Annotations.Entities;
/// <summary>
/// Defines an Entity member name on the property.
/// </summary>
/// <summary>
/// This attribute is used if a storage member has a different name than an Entities'
/// property name. Common case is when Entities are mapped to an existing storage schemas,
/// for example a database where database schema cannot be changed.
/// </summary>
public class MemberAttribute : MappingAttribute
{
	/// <summary>
	/// The name of the property on the storage. If this value is null,
	/// the name of the property is used.
	/// </summary>
	public string? Member { get; set; }
}
