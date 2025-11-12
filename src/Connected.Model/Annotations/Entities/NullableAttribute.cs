namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies if the property can store null values or not regardless of its nullability type definition.
/// </summary>
/// <remarks>
/// Creates a new instance of the NullableAttribute.
/// </remarks>
/// <param name="isNullable">Specifies if the record should allow null values for this property.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NullableAttribute(bool isNullable = true)
		: Attribute
{
	/// <summary>
	/// Gets the value which indicates if the record will allow null values for this properties.
	/// </summary>
	public bool IsNullable { get; } = isNullable;
}
