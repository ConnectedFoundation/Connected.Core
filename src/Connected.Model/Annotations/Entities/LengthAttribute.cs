namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies how much space should storage provider reserve for the value in each record.
/// </summary>
/// <remarks>
/// Creates a new instance of the LengthAttribute.
/// </remarks>
/// <param name="value">The length which depends on the data type. 
/// For strings this is the number of characters, for array it's the number of bytes.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class LengthAttribute(int value)
		: Attribute
{
	/// <summary>
	/// Gets the length that should be reserved by a storage provider in each record.
	/// </summary>
	public int Value { get; } = value;
}
