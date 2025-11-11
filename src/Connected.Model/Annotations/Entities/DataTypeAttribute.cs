using System.Data;

namespace Connected.Annotations.Entities;
/// <summary>
/// Specifies what database type should be used by storage provider when creating
/// a storage schema.
/// </summary>
public sealed class DataTypeAttribute
	: Attribute
{
	/// <summary>
	/// Creates a new instance of the DataTypeAttribute class.
	/// </summary>
	/// <param name="type">The database type that storage provider should use.</param>
	public DataTypeAttribute(DbType type)
	{
		/*
		 * Capture the explicitly declared database type so schema generation can
		 * override default inference rules for the annotated property.
		 */
		Type = type;
	}
	/// <summary>
	/// Gets the database type the storage provider should use for the property.
	/// </summary>
	public DbType Type { get; }
}
