using System.Data;

namespace Connected.Storage.Oracle;

/// <summary>
/// Represents an Oracle database storage operation containing command information and parameters.
/// </summary>
/// <remarks>
/// This sealed class implements <see cref="IStorageOperation"/> to encapsulate all information
/// needed to execute a database command against Oracle databases. It includes the SQL command text,
/// command type (Text, StoredProcedure, TableDirect), parameters, variables, timeout settings,
/// and concurrency mode. The class uses Oracle-specific SQL syntax including bind variables with
/// colon prefix (:param) instead of @ prefix. Operations can represent SELECT queries, INSERT/UPDATE/DELETE
/// commands, or stored procedure calls. The parameters collection supports all parameter directions
/// (Input, Output, InputOutput, ReturnValue) for full Oracle stored procedure integration.
/// </remarks>
internal sealed class OracleStorageOperation
	: IStorageOperation
{
	/// <summary>
	/// Initializes a new instance of the <see cref="OracleStorageOperation"/> class.
	/// </summary>
	public OracleStorageOperation()
	{
		Parameters = [];
		Variables = [];
	}

	/// <summary>
	/// Gets or sets the Oracle SQL command text or stored procedure name.
	/// </summary>
	/// <value>
	/// The SQL statement or stored procedure name to execute. For SQL statements, uses Oracle SQL
	/// syntax with colon-prefixed bind variables (:param). Can be null for operations that don't
	/// require command text.
	/// </value>
	public string? CommandText { get; set; }

	/// <summary>
	/// Gets or sets the type of command to execute.
	/// </summary>
	/// <value>
	/// The <see cref="System.Data.CommandType"/> indicating how to interpret the CommandText.
	/// Defaults to <see cref="CommandType.Text"/> for SQL statements.
	/// </value>
	public CommandType CommandType { get; set; } = CommandType.Text;

	/// <summary>
	/// Gets the list of parameters for the command.
	/// </summary>
	/// <value>
	/// A list of <see cref="IStorageParameter"/> instances representing bind variables for the command.
	/// Parameters use Oracle naming conventions with colon prefix (:param).
	/// </value>
	public List<IStorageParameter> Parameters { get; }

	/// <summary>
	/// Gets the list of variables for the command.
	/// </summary>
	/// <value>
	/// A list of <see cref="IStorageVariable"/> instances for complex command scenarios.
	/// </value>
	public List<IStorageVariable> Variables { get; }

	/// <summary>
	/// Gets or sets the command execution timeout in seconds.
	/// </summary>
	/// <value>
	/// The number of seconds to wait before terminating the command execution.
	/// A value of 0 indicates no timeout limit.
	/// </value>
	public int CommandTimeout { get; set; }

	/// <summary>
	/// Gets or sets the data concurrency mode for the operation.
	/// </summary>
	/// <value>
	/// The <see cref="DataConcurrencyMode"/> indicating whether optimistic concurrency checking
	/// is enabled. When enabled, UPDATE operations include version columns in WHERE clauses.
	/// </value>
	public DataConcurrencyMode Concurrency { get; set; }
}
