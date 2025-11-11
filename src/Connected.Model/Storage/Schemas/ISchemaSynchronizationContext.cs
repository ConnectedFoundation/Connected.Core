namespace Connected.Storage.Schemas;

/// <summary>
/// Provides context information for schema synchronization operations.
/// </summary>
/// <remarks>
/// This interface encapsulates the database connection information required to perform
/// schema synchronization operations. It includes both the connection type (which determines
/// the database provider and SQL dialect) and the connection string (which specifies how
/// to connect to the database). This context is passed through schema synchronization
/// workflows to ensure that operations target the correct database using the appropriate
/// provider-specific logic. The separation of connection type and connection string enables
/// flexible database configuration and supports multiple database providers.
/// </remarks>
public interface ISchemaSynchronizationContext
{
	/// <summary>
	/// Gets the type of the database connection.
	/// </summary>
	/// <value>
	/// A <see cref="Type"/> representing the connection class (e.g., SqlConnection, NpgsqlConnection).
	/// </value>
	/// <remarks>
	/// The connection type identifies which database provider is being used, enabling
	/// schema synchronization logic to apply provider-specific SQL syntax and behaviors.
	/// Different connection types support different database systems such as SQL Server,
	/// PostgreSQL, MySQL, or SQLite.
	/// </remarks>
	Type ConnectionType { get; }

	/// <summary>
	/// Gets the connection string for the database.
	/// </summary>
	/// <value>
	/// A string containing the database connection information.
	/// </value>
	/// <remarks>
	/// The connection string specifies how to connect to the database, including server
	/// address, database name, authentication credentials, and other connection parameters.
	/// This is used to establish connections for schema inspection and synchronization operations.
	/// </remarks>
	string ConnectionString { get; }
}
