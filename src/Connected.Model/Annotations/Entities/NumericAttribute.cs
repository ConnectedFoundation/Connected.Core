namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies how a storage provider should treat a numeric value. Intended for decimal properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NumericAttribute
	: Attribute
{
	/// <summary>
	/// Creates a new instance of the NumericAttribute.
	/// </summary>
	/// <param name="precision">Specifies how precise a numeric value should be stored.</param>
	/// <param name="scale">Specifies how many decimal places the value should store.</param>
	public NumericAttribute(int precision, int scale)
	{
		/*
		 * Persist precision (total digits) and scale (digits after decimal point) so providers can
		 * emit the correct numeric/decimal column definitions.
		 */
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
