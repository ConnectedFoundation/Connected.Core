namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies what date type should be used by storage provider.
/// </summary>
public enum DateKind
{
	/// <summary>
	/// Specific date type is not set. Storage provider can decide
	/// what kind of date it will use.
	/// </summary>
	NotSet = 0,
	/// <summary>
	/// A date data type should be used without time part.
	/// </summary>
	Date = 1,
	/// <summary>
	/// A date and time data type should be used.
	/// </summary>
	DateTime = 2,
	/// <summary>
	/// A high precision date and time data type should be used.
	/// </summary>
	DateTime2 = 3,
	/// <summary>
	/// A low precision date and time data type should be used.
	/// </summary>
	SmallDateTime = 4,
	/// <summary>
	/// A time data type should be used which contains only time without date component.
	/// </summary>
	Time = 5
}
/// <summary>
/// Specifies how the storage provider should create and manage schema for date property.  
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DateAttribute : Attribute
{
	/// <summary>
	/// Creates a new instance of the Date class.
	/// </summary>
	/// <param name="kind">The kind of a date format that should be used when storing Entity's 
	/// records.</param>
	/// <param name="precision">The precision if high precision data type is used. This value might
	/// not be supported by all storage providers.</param>
	public DateAttribute(DateKind kind, int precision = 0)
	{
		/*
		 * Capture both the desired storage kind and precision so providers can emit
		 * an appropriate date/time column definition including scale where supported.
		 */
		Kind = kind;
		Precision = precision;
	}
	/// <summary>
	/// The kind of a date format which defines which parts of the date and time should records hold.
	/// </summary>
	public DateKind Kind { get; }
	/// <summary>
	/// The precision of the high precision types such as DateTime2. 
	/// Higher precision means more acurate values.
	/// </summary>
	public int Precision { get; }
}
