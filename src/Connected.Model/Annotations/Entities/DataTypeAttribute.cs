using System.Data;

namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies what database type should be used by storage provider when creating
/// a storage schema.
/// </summary>
/// <remarks>
/// Creates a new instance of the DataTypeAttribute class.
/// </remarks>
/// <param name="type">The database type that storage provider should use.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DataTypeAttribute(DbType type)
		: Attribute
{
	/// <summary>
	/// Gets the database type the storage provider should use for the property.
	/// </summary>
	public DbType Type { get; } = type;
}
