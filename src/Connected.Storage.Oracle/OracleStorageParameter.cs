using System.Data;

namespace Connected.Storage.Oracle;

/// <summary>
/// Represents a parameter for Oracle storage operations.
/// </summary>
/// <remarks>
/// This sealed class implements <see cref="IStorageParameter"/> to provide Oracle-specific
/// parameter handling for database commands. Parameters are used to safely pass values to SQL
/// statements while preventing SQL injection attacks. The class supports all standard parameter
/// directions (Input, Output, InputOutput, ReturnValue) and leverages the <see cref="DbType"/>
/// enumeration for type specification. Oracle parameter names use colon prefix (:param) in SQL
/// statements but are referenced without the prefix in parameter collections. The class integrates
/// with Oracle.ManagedDataAccess for native Oracle type support including NUMBER, VARCHAR2, CLOB,
/// BLOB, TIMESTAMP, and other Oracle-specific types.
/// </remarks>
internal sealed class OracleStorageParameter
	: IStorageParameter
{
	/// <summary>
	/// Gets or initializes the name of the parameter.
	/// </summary>
	/// <value>
	/// The parameter name, typically without prefix (e.g., "userId" for :userId in SQL).
	/// Can be null for positional parameters.
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
	/// Oracle supports Input, Output, InputOutput, and ReturnValue directions for parameters
	/// in stored procedures and functions. Output and InputOutput parameters are commonly used
	/// with Oracle PL/SQL procedures.
	/// </remarks>
	public ParameterDirection Direction { get; init; } = ParameterDirection.Input;

	/// <summary>
	/// Gets or initializes the database type of the parameter.
	/// </summary>
	/// <value>
	/// The <see cref="DbType"/> that specifies the data type of the parameter. Defaults to <see cref="DbType.String"/>.
	/// </value>
	/// <remarks>
	/// The DbType is used to determine the appropriate Oracle data type during command execution.
	/// Common mappings include String?VARCHAR2/CLOB, Int32?NUMBER(10), Int64?NUMBER(19), and
	/// Guid?RAW(16). Oracle's NUMBER type is used for all numeric types with appropriate precision.
	/// </remarks>
	public DbType Type { get; init; } = DbType.String;
}
