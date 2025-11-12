using Connected.Services;
using Npgsql;
using System.Data;

namespace Connected.Storage.PostgreSql;

/// <summary>
/// Provides PostgreSQL database connection management and command execution capabilities.
/// </summary>
/// <remarks>
/// This abstract class serves as the base for all PostgreSQL storage connections, managing
/// connection lifecycle, transaction handling, and command execution. It uses Npgsql as the
/// underlying PostgreSQL data provider and integrates with the service cancellation framework
/// to support graceful operation cancellation. The class extends <see cref="StorageConnection"/>
/// to provide PostgreSQL-specific implementation of parameter handling and connection creation.
/// It handles Npgsql parameter setup, value mapping, and DBNull conversion for proper database
/// communication.
/// </remarks>
internal abstract class PostgreSqlStorageConnection(ICancellationContext context) : StorageConnection(context)
{
	/// <summary>
	/// Sets up parameters for the database command.
	/// </summary>
	/// <param name="command">The storage command containing parameter information.</param>
	/// <param name="cmd">The database command to configure with parameters.</param>
	/// <remarks>
	/// This method configures Npgsql parameters for the command, either clearing existing
	/// parameters by setting them to DBNull or creating new parameters from the storage
	/// operation's parameter list. Each parameter is configured with its name, type,
	/// direction, and nullability.
	/// </remarks>
	protected override void SetupParameters(IStorageCommand command, IDbCommand cmd)
	{
		if (cmd.Parameters.Count > 0)
		{
			/*
			 * Clear existing parameters by setting to DBNull
			 */
			foreach (NpgsqlParameter i in cmd.Parameters)
				i.Value = DBNull.Value;

			return;
		}

		if (command.Operation.Parameters is null)
			return;

		/*
		 * Create new parameters from operation parameter list
		 */
		foreach (var i in command.Operation.Parameters)
		{
			cmd.Parameters.Add(new NpgsqlParameter
			{
				ParameterName = i.Name,
				DbType = i.Type,
				Direction = i.Direction,
				IsNullable = i.Value is null
			});
		}
	}

	/// <summary>
	/// Gets the value of a parameter from the database command.
	/// </summary>
	/// <param name="command">The database command containing the parameter.</param>
	/// <param name="parameterName">The name of the parameter to retrieve.</param>
	/// <returns>The parameter value, or <c>null</c> if the value is DBNull or command type is invalid.</returns>
	/// <remarks>
	/// This method retrieves the parameter value from an NpgsqlCommand, converting DBNull
	/// values to null for proper C# null handling. This is typically used to retrieve output
	/// parameters such as identity values from INSERT operations.
	/// </remarks>
	protected override object? GetParameterValue(IDbCommand command, string parameterName)
	{
		if (command is NpgsqlCommand cmd)
		{
			var result = cmd.Parameters[parameterName].Value;

			if (result == DBNull.Value)
				return null;

			return result;
		}

		return null;
	}

	/// <summary>
	/// Sets the value of a parameter in the database command.
	/// </summary>
	/// <param name="command">The database command containing the parameter.</param>
	/// <param name="parameterName">The name of the parameter to set.</param>
	/// <param name="value">The value to assign to the parameter, or <c>null</c> for DBNull.</param>
	/// <remarks>
	/// This method sets the parameter value in an NpgsqlCommand, converting null values
	/// to DBNull for proper database handling. This is used when binding parameter values
	/// before command execution.
	/// </remarks>
	protected override void SetParameterValue(IDbCommand command, string parameterName, object? value)
	{
		if (command is NpgsqlCommand cmd)
			cmd.Parameters[parameterName].Value = value is null ? DBNull.Value : value;
	}

	/// <summary>
	/// Creates a new PostgreSQL database connection.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// the <see cref="IDbConnection"/> instance configured with the connection string.
	/// </returns>
	/// <remarks>
	/// This method creates a new NpgsqlConnection instance using the configured connection
	/// string. The connection is not opened by this method; it will be opened when first
	/// used by the connection manager.
	/// </remarks>
	protected override async Task<IDbConnection> OnCreateConnection()
	{
		await Task.CompletedTask;

		return new NpgsqlConnection(ConnectionString);
	}

	/// <summary>
	/// Resolves the PostgreSQL server version from the connection string.
	/// </summary>
	/// <param name="connectionString">The connection string containing server information.</param>
	/// <returns>The PostgreSQL server version.</returns>
	/// <remarks>
	/// This method opens a temporary connection to the PostgreSQL server to retrieve the
	/// server version. If the connection fails, it returns a default version (15.0).
	/// The version information can be used for version-specific SQL syntax or feature detection.
	/// </remarks>
	public static Version ResolveVersion(string connectionString)
	{
		try
		{
			using var connection = new NpgsqlConnection(connectionString);
			connection.Open();

			var version = connection.ServerVersion;
			connection.Close();

			return new Version(version);
		}
		catch
		{
			/*
			 * Return a default PostgreSQL version if unable to determine actual version
			 */
			return new Version(15, 0);
		}
	}
}
