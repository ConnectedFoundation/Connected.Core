namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies what storage binary type should be used.
/// </summary>
public enum BinaryKind
{
	/// <summary>
	/// Storage should use variable length binary storage.
	/// </summary>
	VarBinary = 0,
	/// <summary>
	/// Storage should use fixed length binary storage.
	/// </summary>
	Binary = 1
}
/// <summary>
/// Specifies that a storage should use a binary data type.
/// </summary>
/// <remarks>
/// Use this attribute for byte arrays or similar data structures that can be
/// serialized to binary data type. This attribute is typically used with 
/// LengthAttribute which defines the maximum size of the data.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class BinaryAttribute : Attribute
{
	/// <summary>
	/// Creates a new instance of the BinaryAttribute class.
	/// </summary>
	/// <param name="kind">The kind of a binary type that should be used by storage provider.</param>
	public BinaryAttribute(BinaryKind kind)
	{
		Kind = kind;
	}
	/// <summary>
	/// Specifies what kind of binary type the should use.
	/// </summary>
	public BinaryKind Kind { get; }
}
