namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies how a storage provider should treat a numeric value.
/// </summary>
/// <summary>
/// This attribute should be used if the property is of decimal type.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NumericAttribute : Attribute
{
	/// <summary>
	/// Creates a new instance of the NumericAttribute.
	/// </summary>
	/// <param name="precision">Specifies how precise a numeric value should be stored.</param>
	/// <param name="scale">Specifies how many decimal places the value should store.</param>
	public NumericAttribute(int precision, int scale)
	{
		Precision = precision;
		Scale = scale;
	}
	/// <summary>
	/// Gets the precision for the numeric value.
	/// </summary>
	public int Precision { get; set; }
	/// <summary>
	/// Gets the number of decimal places for the numeric value.
	/// </summary>
	public int Scale { get; set; }
}
