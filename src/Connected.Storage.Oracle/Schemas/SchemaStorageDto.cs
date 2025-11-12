using Connected.Storage.Schemas;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Data transfer object for Oracle schema storage operations containing connection and operation context.
/// </summary>
/// <remarks>
/// This sealed record encapsulates all information needed to execute storage operations during
/// schema synchronization including the connection type, connection string, storage operation details,
/// and connection mode. It implements both <see cref="IStorageContextDto"/> for general storage
/// context and <see cref="ISchemaSynchronizationContext"/> for schema-specific operations. The DTO
/// is used to pass context information to storage providers during DDL execution, ensuring operations
/// are executed with the correct connection settings and operational parameters. Oracle DDL statements
/// are automatically committed and cannot be rolled back, so connection mode settings ensure proper
/// isolation during schema modifications. The default connection mode is Shared for efficient connection
/// pooling during schema operations.
/// </remarks>
internal sealed class SchemaStorageDto(Type connectionType, string connectionString, IStorageOperation operation)
	: IStorageContextDto, ISchemaSynchronizationContext
{
	/// <summary>
	/// Gets or sets the connection type for the storage operation.
	/// </summary>
	/// <value>
	/// The type implementing the database connection (typically <see cref="OracleDataConnection"/>).
	/// </value>
	public Type ConnectionType { get; set; } = connectionType;

	/// <summary>
	/// Gets or sets the connection string for the Oracle database.
	/// </summary>
	/// <value>
	/// The connection string in Oracle format (Easy Connect, TNS Names, or full descriptor).
	/// </value>
	/// <remarks>
	/// Oracle connection strings support multiple formats:
	/// - Easy Connect: Host=localhost:1521/XEPDB1;User Id=system;Password=oracle;
	/// - TNS Names: Data Source=MYDB;User Id=system;Password=oracle;
	/// - Full Descriptor: Data Source=(DESCRIPTION=...);User Id=system;Password=oracle;
	/// </remarks>
	public string ConnectionString { get; set; } = connectionString;

	/// <summary>
	/// Gets or sets the storage operation to execute.
	/// </summary>
	/// <value>
	/// The operation containing DDL command text, parameters, and execution settings.
	/// </value>
	public IStorageOperation Operation { get; set; } = operation;

	/// <summary>
	/// Gets or sets the connection mode for the operation.
	/// </summary>
	/// <value>
	/// The connection mode determining connection pooling and reuse behavior.
	/// </value>
	/// <remarks>
	/// Oracle connection pooling is managed by Oracle.ManagedDataAccess.Core and supports
	/// both shared and isolated connections. DDL operations are auto-committed regardless
	/// of connection mode.
	/// </remarks>
	public StorageConnectionMode ConnectionMode { get; set; } = StorageConnectionMode.Shared;
}
