namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies what kind of string value a storage provider should reserve for the Entity's property.
/// </summary>
public enum StringKind
{
	/// <summary>
	/// Unicode character set should be used with a variable length.
	/// </summary>
	NVarChar = 0,
	/// <summary>
	/// Single byte character should be used with a variable length.
	/// </summary>
	VarChar = 1,
	/// <summary>
	/// Single byte character should be used with a fixed length.
	/// </summary>
	Char = 2,
	/// <summary>
	/// Unicode character set should be used with a fixed length.
	/// </summary>
	NChar = 3
}

/// <summary>
/// Creates a new instance of the StringAttribute class.
/// </summary>
/// <param name="kind">Specifies how string value will be treated by a storage provider.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class StringAttribute(StringKind kind)
		: Attribute
{
	/// <summary>
	/// Get a value indicating how a storage provider should treat a string value.
	/// </summary>
	public StringKind Kind { get; } = kind;
}
