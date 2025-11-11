namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies if the property can store null values or not regardless of its nullability type definition.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NullableAttribute
	: Attribute
{
	/// <summary>
	/// Creates a new instance of the NullableAttribute.
	/// </summary>
	/// <param name="isNullable">Specifies if the record should allow null values for this property.</param>
	public NullableAttribute(bool isNullable = true)
	{
		/*
		 * Persist the desired nullability override so schema generators can force allow/disallow
		 * NULL regardless of the CLR type's inherent nullability.
		 */
		IsNullable = isNullable;
	}
	/// <summary>
	/// Gets the value which indicates if the record will allow null values for this properties.
	/// </summary>
	public bool IsNullable { get; }
}
