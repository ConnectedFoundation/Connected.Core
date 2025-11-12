namespace Connected.Storage.Schemas;

/// <summary>
/// Represents a database schema containing a collection of tables.
/// </summary>
/// <remarks>
/// This interface provides access to the complete database schema definition, including
/// all tables and their metadata. It serves as the top-level container for database schema
/// information, enabling schema inspection, validation, and synchronization operations.
/// The database schema representation is used during application initialization to ensure
/// database structures match the expected entity models and to perform schema migrations
/// or updates when needed.
/// </remarks>
public interface IDatabase
{
	/// <summary>
	/// Gets the collection of tables in the database.
	/// </summary>
	/// <value>
	/// A list containing all table definitions in the database.
	/// </value>
	/// <remarks>
	/// This property provides access to all table schemas in the database, including their
	/// columns, indexes, constraints, and other metadata. The list can be used for schema
	/// comparison, validation, and migration operations.
	/// </remarks>
	List<ITable> Tables { get; }
}
