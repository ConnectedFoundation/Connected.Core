using Connected.Configuration;
using Connected.Storage.Containers;
using Npgsql;

namespace Connected.Storage.PostgreSql.Containers;

/// <summary>
/// Manages PostgreSQL storage containers: each container is a database on the default database's server.
/// </summary>
/// <remarks>
/// Database commands run on the server's <c>postgres</c> maintenance database. Dropping forces other sessions off,
/// which needs PostgreSQL 13 or later.
/// </remarks>
internal sealed class PostgreSqlStorageContainerProvider(IConfigurationService configuration)
	: StorageContainerProvider(configuration)
{
	/// <inheritdoc/>
	public override Type ConnectionType => typeof(PostgreSqlDataConnection);

	/// <inheritdoc/>
	protected override string DefaultName() => new NpgsqlConnectionStringBuilder(DefaultConnectionString).Database ?? "";

	/// <inheritdoc/>
	protected override async Task<string> OnCreate(string name)
	{
		await using var connection = await Open();
		await using var exists = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);

		exists.Parameters.AddWithValue("name", name);

		/*
		 * PostgreSQL has no conditional CREATE DATABASE, and the statement cannot run inside a transaction block.
		 */
		if (await exists.ExecuteScalarAsync(CancellationToken) is null)
		{
			await using var create = new NpgsqlCommand($"CREATE DATABASE {Quote(name)}", connection);

			await create.ExecuteNonQueryAsync(CancellationToken);
		}

		return new NpgsqlConnectionStringBuilder(DefaultConnectionString) { Database = name }.ConnectionString;
	}

	/// <inheritdoc/>
	protected override async Task OnDrop(string name)
	{
		/*
		 * Pooled connections would keep the database in use.
		 */
		NpgsqlConnection.ClearPool(new NpgsqlConnection(new NpgsqlConnectionStringBuilder(DefaultConnectionString) { Database = name }.ConnectionString));

		await using var connection = await Open();
		await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {Quote(name)} WITH (FORCE)", connection);

		await drop.ExecuteNonQueryAsync(CancellationToken);
	}

	private async Task<NpgsqlConnection> Open()
	{
		var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(DefaultConnectionString) { Database = "postgres" }.ConnectionString);

		await connection.OpenAsync(CancellationToken);

		return connection;
	}

	private static string Quote(string name) => $"\"{name.Replace("\"", "\"\"")}\"";
}
