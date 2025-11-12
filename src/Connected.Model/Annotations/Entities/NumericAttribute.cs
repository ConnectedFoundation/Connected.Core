namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies how a storage provider should treat a numeric value. Intended for decimal properties.
/// </summary>
/// <remarks>
/// Creates a new instance of the NumericAttribute.
/// </remarks>
/// <param name="precision">Specifies how precise a numeric value should be stored.</param>
/// <param name="scale">Specifies how many decimal places the value should store.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NumericAttribute(int precision, int scale)
		: Attribute
{
	/// <summary>
	/// Gets the precision for the numeric value.
	/// </summary>
	public int Precision { get; set; } = precision;
	/// <summary>
	/// Gets the number of decimal places for the numeric value.
	/// </summary>
	public int Scale { get; set; } = scale;
}
