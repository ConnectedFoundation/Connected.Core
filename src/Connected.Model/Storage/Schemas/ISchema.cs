namespace Connected.Storage.Schemas;

/// <summary>
/// Represents the base schema definition for database objects.
/// </summary>
/// <remarks>
/// This interface provides the fundamental metadata common to all database schema objects
/// such as tables, views, and constraints. It includes information about the object's
/// location (schema and name), type, columns, and whether it should be ignored during
/// schema operations. The interface implements <see cref="IEquatable{T}"/> to support
/// schema comparison operations essential for schema synchronization and validation.
/// Schema definitions serve as the blueprint for database structure management throughout
/// the application lifecycle.
/// </remarks>
public interface ISchema
	: IEquatable<ISchema>
{
	/// <summary>
	/// Gets the collection of columns defined in this schema object.
	/// </summary>
	/// <value>
	/// A list containing all column definitions for this schema object.
	/// </value>
	/// <remarks>
	/// The columns collection provides access to all column metadata including data types,
	/// constraints, and properties. This is used for schema comparison, validation, and
	/// migration operations.
	/// </remarks>
	List<ISchemaColumn> Columns { get; }

	/// <summary>
	/// Gets the schema name where this object resides.
	/// </summary>
	/// <value>
	/// A string representing the database schema name.
	/// </value>
	/// <remarks>
	/// The schema name identifies the logical grouping or namespace within the database
	/// where this object is located, such as "dbo" or custom schema names.
	/// </remarks>
	string Schema { get; }

	/// <summary>
	/// Gets the name of this schema object.
	/// </summary>
	/// <value>
	/// A string representing the object name.
	/// </value>
	/// <remarks>
	/// The name uniquely identifies the schema object within its schema, such as a
	/// table name or view name.
	/// </remarks>
	string Name { get; }

	/// <summary>
	/// Gets the type of this schema object.
	/// </summary>
	/// <value>
	/// A string representing the object type (e.g., "TABLE", "VIEW", "CONSTRAINT").
	/// </value>
	/// <remarks>
	/// The type categorizes the schema object, enabling type-specific processing during
	/// schema synchronization and validation operations.
	/// </remarks>
	string Type { get; }

	/// <summary>
	/// Gets a value indicating whether this schema object should be ignored during schema operations.
	/// </summary>
	/// <value>
	/// <c>true</c> if the schema object should be ignored; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	/// When set to true, this schema object is excluded from schema synchronization,
	/// validation, and migration operations. This is useful for system tables or
	/// objects managed externally.
	/// </remarks>
	bool Ignore { get; }
}
