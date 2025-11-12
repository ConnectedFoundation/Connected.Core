namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies if the property value should be automatically set after the storage transaction performs insert.
/// </summary>
/// <remarks>
/// Storage transactions return a newly inserted identity value if one of the properties is decorated
/// with PrimaryKey (identity = true) attribute. This attribute is typically set on the same property
/// since it's actual the value of the primary key which is returned from the transaction.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ReturnValueAttribute
	: Attribute
{
	/*
	 * Marker attribute: indicates that a property's value should be populated from a return
	 * value (such as an identity) after inserts.
	 */
}
