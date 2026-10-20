using Connected.Configuration;
using Connected.Storage.Containers;
using Npgsql;

namespace Connected.Storage.PostgreSql.Containers;

/// <summary>
/// Manages PostgreSQL storage containers: each container is a database on the default database's server, marked with
/// a database comment.
/// </summary>
/// <remarks>
/// Database commands run on the default database, which is never a container, so the login needs no access to the
/// server's <c>postgres</c> maintenance database. Dropping forces other sessions off, which needs PostgreSQL 13 or later.
/// </remarks>
internal sealed class PostgreSqlStorageContainerProvider(IConfigurationService configuration)
	: StorageContainerProvider(configuration)
{
	/// <inheritdoc/>
	public override Type ConnectionType => typeof(PostgreSqlDataConnection);

	/// <inheritdoc/>
	protected override async Task<string?> DefaultName()
	{
		await using var connection = new NpgsqlConnection(DefaultConnectionString);

		await connection.OpenAsync(CancellationToken);

		await using var command = new NpgsqlCommand("SELECT current_database()", connection);

		return await command.ExecuteScalarAsync(CancellationToken) as string;
	}

	/// <inheritdoc/>
	protected override async Task<string> OnCreate(string name)
	{
		await using var connection = await Open();

		/*
		 * PostgreSQL has no conditional CREATE DATABASE, and the statement cannot run inside a transaction block.
		 */
		switch (await Probe(name, connection))
		{
			case null:
				await CreateMarked(name, connection);
				break;
			case false:
				throw NotAContainer(name);
		}

		return new NpgsqlConnectionStringBuilder(DefaultConnectionString) { Database = name }.ConnectionString;
	}

	/// <inheritdoc/>
	protected override async Task OnDrop(string name)
	{
		await using var connection = await Open();

		switch (await Probe(name, connection))
		{
			case null:
				return;
			case false:
				throw NotAContainer(name);
		}

		/*
		 * Pooled connections would keep the database in use.
		 */
		NpgsqlConnection.ClearPool(new NpgsqlConnection(new NpgsqlConnectionStringBuilder(DefaultConnectionString) { Database = name }.ConnectionString));

		await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {Quote(name)} WITH (FORCE)", connection);

		await drop.ExecuteNonQueryAsync(CancellationToken);
	}

	/// <summary>
	/// Creates and marks the database under a staging name and only then renames it, so the container's name never
	/// belongs to an unmarked database. A failure drops the staging database; two concurrent creates race on the
	/// rename, and the one that loses finds the other's container. The steps are not cancelled midway.
	/// </summary>
	private async Task CreateMarked(string name, NpgsqlConnection connection)
	{
		var staging = $"connected_staging_{Guid.NewGuid():N}";

		await using (var create = new NpgsqlCommand($"CREATE DATABASE {Quote(staging)}", connection))
			await create.ExecuteNonQueryAsync(CancellationToken.None);

		try
		{
			await using (var mark = new NpgsqlCommand($"COMMENT ON DATABASE {Quote(staging)} IS '{ContainerMarker}'", connection))
				await mark.ExecuteNonQueryAsync(CancellationToken.None);

			await using var rename = new NpgsqlCommand($"ALTER DATABASE {Quote(staging)} RENAME TO {Quote(name)}", connection);

			await rename.ExecuteNonQueryAsync(CancellationToken.None);
		}
		catch (PostgresException ex)
		{
			try
			{
				await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {Quote(staging)}", connection);

				await drop.ExecuteNonQueryAsync(CancellationToken.None);
			}
			catch (PostgresException)
			{
				/*
				 * The original failure is the one worth reporting; a staging database left behind is marked and
				 * holds no name an application uses.
				 */
			}

			if (ex.SqlState != PostgresErrorCodes.DuplicateDatabase)
				throw;

			if (await Probe(name, connection) != true)
				throw NotAContainer(name);
		}
	}

	/// <returns><c>null</c> if no database has the name, otherwise whether it is a container.</returns>
	private async Task<bool?> Probe(string name, NpgsqlConnection connection)
	{
		await using var command = new NpgsqlCommand("SELECT coalesce(shobj_description(oid, 'pg_database'), '') FROM pg_database WHERE datname = @name", connection);

		command.Parameters.AddWithValue("name", name);

		return await command.ExecuteScalarAsync(CancellationToken) is string comment ? comment == ContainerMarker : null;
	}

	private async Task<NpgsqlConnection> Open()
	{
		var connection = new NpgsqlConnection(DefaultConnectionString);

		await connection.OpenAsync(CancellationToken);

		return connection;
	}

	private static string Quote(string name) => $"\"{name.Replace("\"", "\"\"")}\"";
}
