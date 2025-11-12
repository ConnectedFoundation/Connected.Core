using System.Data;

namespace Connected.Storage.PostgreSql;

/// <summary>
/// Represents a parameter for PostgreSQL storage operations.
/// </summary>
/// <remarks>
/// This sealed class implements <see cref="IStorageParameter"/> to provide PostgreSQL-specific
/// parameter handling for database commands. Parameters are used to safely pass values to SQL
/// statements while preventing SQL injection attacks. The class supports all standard parameter
/// directions (Input, Output, InputOutput, ReturnValue) and leverages the <see cref="DbType"/>
/// enumeration for type specification. PostgreSQL parameter names typically use lowercase
/// identifiers and can be referenced with the @ prefix in SQL statements.
/// </remarks>
internal sealed class PostgreSqlStorageParameter
	: IStorageParameter
{
	/// <summary>
	/// Gets or initializes the name of the parameter.
	/// </summary>
	/// <value>
	/// The parameter name, typically prefixed with @ (e.g., "@userId"). Can be null for positional parameters.
	/// </value>
	public string? Name { get; init; }

	/// <summary>
	/// Gets or sets the value of the parameter.
	/// </summary>
	/// <value>
	/// The parameter value to be passed to the database command. Can be null for NULL values.
	/// </value>
	public object? Value { get; set; }

	/// <summary>
	/// Gets or initializes the direction of the parameter.
	/// </summary>
	/// <value>
	/// The parameter direction indicating how the parameter is used. Defaults to <see cref="ParameterDirection.Input"/>.
	/// </value>
	/// <remarks>
	/// PostgreSQL supports Input, Output, InputOutput, and ReturnValue directions for parameters
	/// in stored procedures and functions.
	/// </remarks>
	public ParameterDirection Direction { get; init; } = ParameterDirection.Input;

	/// <summary>
	/// Gets or initializes the database type of the parameter.
	/// </summary>
	/// <value>
	/// The <see cref="DbType"/> that specifies the data type of the parameter. Defaults to <see cref="DbType.String"/>.
	/// </value>
	/// <remarks>
	/// The DbType is used to determine the appropriate PostgreSQL data type during command execution.
	/// Common mappings include String?VARCHAR/TEXT, Int32?INTEGER, Int64?BIGINT, and Guid?UUID.
	/// </remarks>
	public DbType Type { get; init; } = DbType.String;
}
