using Connected.Configuration;
using Connected.Storage.Containers;
using Microsoft.Data.SqlClient;

namespace Connected.Storage.Sql.Containers;

/// <summary>
/// Manages SQL Server storage containers: each container is a database on the default database's server, marked with
/// a database-level extended property.
/// </summary>
internal sealed class SqlStorageContainerProvider(IConfigurationService configuration)
	: StorageContainerProvider(configuration)
{
	/// <inheritdoc/>
	public override Type ConnectionType => typeof(SqlDataConnection);

	/// <inheritdoc/>
	protected override async Task<string?> DefaultName()
	{
		await using var connection = new SqlConnection(DefaultConnectionString);

		await connection.OpenAsync(CancellationToken);

		await using var command = new SqlCommand("SELECT DB_NAME()", connection);

		return await command.ExecuteScalarAsync(CancellationToken) as string;
	}

	/// <inheritdoc/>
	protected override async Task<string> OnCreate(string name)
	{
		if (await Exists(name))
		{
			if (!await IsContainer(name))
				throw NotAContainer(name);
		}
		else
		{
			await Execute($"CREATE DATABASE {Quote(name)}", name);
			await Execute($"EXEC {Quote(name)}.sys.sp_addextendedproperty @name = @marker, @value = N'true'", name);
		}

		return new SqlConnectionStringBuilder(DefaultConnectionString) { InitialCatalog = name }.ConnectionString;
	}

	/// <inheritdoc/>
	protected override async Task OnDrop(string name)
	{
		if (!await Exists(name))
			return;

		if (!await IsContainer(name))
			throw NotAContainer(name);

		/*
		 * Pooled connections would keep the database in use; open sessions are rolled back so the drop cannot wait.
		 * Taking the database offline rather than to single user means a client reconnecting before the drop can only
		 * make the drop fail, never leave the database stuck in single user mode.
		 */
		SqlConnection.ClearPool(new SqlConnection(new SqlConnectionStringBuilder(DefaultConnectionString) { InitialCatalog = name }.ConnectionString));

		await Execute($"""
			ALTER DATABASE {Quote(name)} SET OFFLINE WITH ROLLBACK IMMEDIATE;
			ALTER DATABASE {Quote(name)} SET ONLINE;
			DROP DATABASE {Quote(name)};
			""", name);
	}

	private async Task<bool> Exists(string name)
	{
		return await Scalar("SELECT DB_ID(@name)", name) is not (null or DBNull);
	}

	private async Task<bool> IsContainer(string name)
	{
		return await Scalar($"SELECT 1 FROM {Quote(name)}.sys.extended_properties WHERE class = 0 AND name = @marker", name) is not (null or DBNull);
	}

	private async Task Execute(string text, string name)
	{
		await using var connection = await Open();
		await using var command = CreateCommand(text, name, connection);

		await command.ExecuteNonQueryAsync(CancellationToken);
	}

	private async Task<object?> Scalar(string text, string name)
	{
		await using var connection = await Open();
		await using var command = CreateCommand(text, name, connection);

		return await command.ExecuteScalarAsync(CancellationToken);
	}

	private async Task<SqlConnection> Open()
	{
		var connection = new SqlConnection(new SqlConnectionStringBuilder(DefaultConnectionString) { InitialCatalog = "master" }.ConnectionString);

		await connection.OpenAsync(CancellationToken);

		return connection;
	}

	private static SqlCommand CreateCommand(string text, string name, SqlConnection connection)
	{
		var command = new SqlCommand(text, connection);

		command.Parameters.AddWithValue("@name", name);
		command.Parameters.AddWithValue("@marker", ContainerMarker);

		return command;
	}

	private static string Quote(string name) => $"[{name.Replace("]", "]]")}]";
}
