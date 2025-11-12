using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Data transfer object for schema storage operations containing connection and operation context.
/// </summary>
/// <remarks>
/// This sealed record encapsulates all information needed to execute storage operations during
/// schema synchronization including the connection type, connection string, storage operation details,
/// and connection mode. It implements both <see cref="IStorageContextDto"/> for general storage
/// context and <see cref="ISchemaSynchronizationContext"/> for schema-specific operations. The DTO
/// is used to pass context information to storage providers during DDL execution, ensuring operations
/// are executed with the correct connection settings and operational parameters. The default connection
/// mode is Shared, allowing connection pooling for efficient resource utilization during schema operations.
/// </remarks>
internal sealed class SchemaStorageDto(Type connectionType, string connectionString, IStorageOperation operation)
	: IStorageContextDto, ISchemaSynchronizationContext
{
	/// <summary>
	/// Gets or sets the connection type for the storage operation.
	/// </summary>
	/// <value>
	/// The type implementing the database connection.
	/// </value>
	public Type ConnectionType { get; set; } = connectionType;

	/// <summary>
	/// Gets or sets the connection string for the database.
	/// </summary>
	/// <value>
	/// The connection string containing server, database, and authentication information.
	/// </value>
	public string ConnectionString { get; set; } = connectionString;

	/// <summary>
	/// Gets or sets the storage operation to execute.
	/// </summary>
	/// <value>
	/// The operation containing command text, parameters, and execution settings.
	/// </value>
	public IStorageOperation Operation { get; set; } = operation;

	/// <summary>
	/// Gets or sets the connection mode for the operation.
	/// </summary>
	/// <value>
	/// The connection mode determining connection pooling and reuse behavior.
	/// </value>
	public StorageConnectionMode ConnectionMode { get; set; } = StorageConnectionMode.Shared;
}
