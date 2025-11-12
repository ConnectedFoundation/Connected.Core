namespace Connected.Annotations.Entities;

/// <summary>
/// Specifies what date type should be used by storage provider.
/// </summary>
/// <remarks>
/// This enumeration allows fine-grained control over how date and time values are stored
/// in the underlying storage provider. Different storage providers may interpret these
/// values differently based on their native date/time type support.
/// </remarks>
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
	Time = 5,
	/// <summary>
	/// A date and time data type with timezone offset information should be used.
	/// </summary>
	Offset = 6
}

/// <summary>
/// Specifies how the storage provider should create and manage schema for date property.  
/// </summary>
/// <remarks>
/// This attribute provides metadata to storage providers about how date and time values
/// should be persisted. It supports various date formats and precision levels, allowing
/// developers to optimize storage based on their specific requirements. The precision
/// parameter is particularly useful for high-precision scenarios where millisecond or
/// sub-millisecond accuracy is required.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DateAttribute(DateKind kind, int precision = 0) : Attribute
{
	/// <summary>
	/// The kind of a date format which defines which parts of the date and time should records hold.
	/// </summary>
	public DateKind Kind { get; } = kind;

	/// <summary>
	/// The precision of the high precision types such as DateTime2. 
	/// Higher precision means more accurate values.
	/// </summary>
	/// <remarks>
	/// This value might not be supported by all storage providers. When supported, it typically
	/// represents the number of decimal places for fractional seconds. Common values range from
	/// 0 to 7, where 0 means no fractional seconds and 7 provides the highest precision.
	/// </remarks>
	public int Precision { get; } = precision;
}
