namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies how much space should storage provider reserve for the value in each record.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class LengthAttribute
	: Attribute
{
	/// <summary>
	/// Creates a new instance of the LengthAttribute.
	/// </summary>
	/// <param name="value">The length which depends on the data type. 
	/// For strings this is the number of characters, for array it's the number of bytes.</param>
	public LengthAttribute(int value)
	{
		/*
		 * Store the requested maximum length so providers can create appropriate column
		 * definitions (character count for strings or byte count for binary arrays).
		 */
		Value = value;
	}
	/// <summary>
	/// Gets the length that should be reserved by a storage provider in each record.
	/// </summary>
	public int Value { get; }
}
