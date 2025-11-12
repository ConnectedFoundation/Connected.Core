using System.Data;

namespace Connected.Storage.PostgreSql;

/// <summary>
/// Represents a storage operation for PostgreSQL databases.
/// </summary>
/// <remarks>
/// This class encapsulates all information needed to execute a command against a PostgreSQL database
/// including the command text, parameters, command type, and execution settings. It implements
/// <see cref="IStorageOperation"/> to provide a consistent interface for database operations across
/// different storage providers. The operation supports parameterized queries, variables, concurrency
/// control modes, and custom timeout settings for PostgreSQL-specific execution requirements.
/// </remarks>
internal class PostgreSqlStorageOperation
	: IStorageOperation
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PostgreSqlStorageOperation"/> class.
	/// </summary>
	public PostgreSqlStorageOperation()
	{
		Parameters = [];
		Variables = [];
	}

	/// <summary>
	/// Gets the list of parameters for the database command.
	/// </summary>
	public List<IStorageParameter> Parameters { get; }

	/// <summary>
	/// Gets the list of variables for the database command.
	/// </summary>
	public List<IStorageVariable> Variables { get; }

	/// <summary>
	/// Gets or sets the command text to execute.
	/// </summary>
	public string? CommandText { get; set; }

	/// <summary>
	/// Gets or sets the command type indicating how the command text should be interpreted.
	/// </summary>
	public CommandType CommandType { get; set; } = CommandType.Text;

	/// <summary>
	/// Gets or sets the data concurrency mode for the operation.
	/// </summary>
	public DataConcurrencyMode Concurrency { get; set; } = DataConcurrencyMode.Disabled;

	/// <summary>
	/// Gets or sets the command execution timeout in seconds.
	/// </summary>
	public int CommandTimeout { get; set; }
}
