using Connected.Services;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Connected.Storage.Sql;

public abstract class SqlStorageConnection(ICancellationContext context) : StorageConnection(context)
{
	protected override void SetupParameters(IStorageCommand command, IDbCommand cmd)
	{
		if (cmd.Parameters.Count > 0)
		{
			foreach (SqlParameter i in cmd.Parameters)
				i.Value = DBNull.Value;

			return;
		}

		if (command.Operation.Parameters is null)
			return;

		foreach (var i in command.Operation.Parameters)
		{
			cmd.Parameters.Add(new SqlParameter
			{
				ParameterName = i.Name,
				DbType = i.Type,
				Direction = i.Direction,
				IsNullable = i.Value is null
			});
		}
	}

	protected override object? GetParameterValue(IDbCommand command, string parameterName)
	{
		if (command is SqlCommand cmd)
		{
			var result = cmd.Parameters[parameterName].Value;

			if (result == DBNull.Value)
				return null;

			return result;
		}

		return null;
	}

	protected override void SetParameterValue(IDbCommand command, string parameterName, object? value)
	{
		if (command is SqlCommand cmd)
			cmd.Parameters[parameterName].Value = value is null ? DBNull.Value : value;
	}

	protected override async Task<IDbConnection> OnCreateConnection()
	{
		await Task.CompletedTask;

		return new SqlConnection(ConnectionString);
	}

	public static Version ResolveVersion(string connectionString)
	{
		Version result;
		using var connection = new SqlConnection(connectionString);

		connection.Open();

		try
		{
			result = Version.Parse(connection.ServerVersion);
		}
		finally
		{
			connection.Close();
		}

		return result;
	}
}
