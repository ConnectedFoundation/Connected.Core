using Connected.Configuration;
using Connected.Storage.Containers;
using Microsoft.Data.SqlClient;

namespace Connected.Storage.Sql.Containers;

/// <summary>
/// Manages SQL Server storage containers: each container is a database on the default database's server, marked with
/// a database-level extended property.
/// </summary>
/// <remarks>
/// Database names starting with <c>connected_staging_</c> and <c>connected_dropping_</c> are the provider's own: a
/// container is created under the first and renamed when it is complete, and renamed to the second before it is dropped.
/// The provider never changes the state of a database under any other name.
/// </remarks>
internal sealed class SqlStorageContainerProvider(IConfigurationService configuration)
	: StorageContainerProvider(configuration)
{
	/// <inheritdoc/>
	public override Type ConnectionType => typeof(SqlDataConnection);

	/// <summary>
	/// The error a create or rename raises when a database with the name exists.
	/// </summary>
	private const int DatabaseExists = 1801;

	/// <summary>
	/// The engine edition <c>SERVERPROPERTY('EngineEdition')</c> reports for Azure SQL Database.
	/// </summary>
	private const int AzureSqlDatabase = 5;

	/// <summary>
	/// The error a rename raises when another session uses the database.
	/// </summary>
	private const int ExclusiveLockFailed = 5030;

	private const int RenameAttempts = 3;

	private const string StagingPrefix = "connected_staging_";
	private const string DroppingPrefix = "connected_dropping_";

	/// <summary>
	/// Whether the server is supported, once known; the engine edition never changes for a connection string.
	/// </summary>
	private bool? _supported;

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
		await using var connection = await Open();

		await EnsureSupported(connection);

		switch (await Probe(name, connection))
		{
			case null:
				await CreateMarked(name, connection);
				break;
			case false:
				throw NotAContainer(name);
			case true:
				/*
				 * A drop interrupted before its rename leaves the container with restricted access.
				 */
				if (await Scalar("SELECT user_access_desc FROM sys.databases WHERE name = @name", name, connection) is string access
					&& !string.Equals(access, "MULTI_USER", StringComparison.OrdinalIgnoreCase))
					await Execute($"ALTER DATABASE {Quote(name)} SET MULTI_USER", name, connection, uninterruptible: true);
				break;
		}

		return new SqlConnectionStringBuilder(DefaultConnectionString) { InitialCatalog = name }.ConnectionString;
	}

	/// <inheritdoc/>
	protected override async Task OnDrop(string name)
	{
		await using var connection = await Open();

		await EnsureSupported(connection);
		await DropAbandoned(connection);

		switch (await Probe(name, connection))
		{
			case null:
				return;
			case false:
				throw NotAContainer(name);
		}

		var dropping = $"{DroppingPrefix}{Guid.NewGuid():N}";
		/*
		 * The container is renamed first, so its name is free as soon as the drop starts and nothing that follows can
		 * leave a database under it in a state other than online. A rename needs the database to itself, so open
		 * sessions are rolled back by restricting access, which keeps it online; a session that slips back in before
		 * the rename is rolled back on the next attempt. Once renamed, clients reconnecting by the old name fail, so
		 * the database can be taken to single user and dropped without a client taking the one connection allowed.
		 * The steps run without a timeout and are not cancelled midway; a drop interrupted anyway leaves either the
		 * container, online and still marked, or a database only this provider names, which the next drop removes.
		 */
		SqlConnection.ClearPool(new SqlConnection(new SqlConnectionStringBuilder(DefaultConnectionString) { InitialCatalog = name }.ConnectionString));

		await Rename(name, dropping, connection);
		await Remove(dropping, "ONLINE", connection);
	}

	private async Task Rename(string name, string dropping, SqlConnection connection)
	{
		for (var attempt = 1; ; attempt++)
		{
			try
			{
				await Execute($"""
					ALTER DATABASE {Quote(name)} SET RESTRICTED_USER WITH ROLLBACK IMMEDIATE;
					ALTER DATABASE {Quote(name)} MODIFY NAME = {Quote(dropping)};
					""", name, connection, uninterruptible: true);

				return;
			}
			catch (SqlException ex) when (ex.Number == ExclusiveLockFailed)
			{
				if (attempt < RenameAttempts)
					continue;

				await Execute($"ALTER DATABASE {Quote(name)} SET MULTI_USER", name, connection, uninterruptible: true);

				throw;
			}
		}
	}

	/// <summary>
	/// Creates and marks the database under a staging name and only then renames it, so the container's name never
	/// belongs to an unmarked database. A failure drops the staging database; two concurrent creates race on the
	/// rename, and the one that loses finds the other's container.
	/// </summary>
	private async Task CreateMarked(string name, SqlConnection connection)
	{
		var staging = $"{StagingPrefix}{Guid.NewGuid():N}";

		await Execute($"CREATE DATABASE {Quote(staging)}", staging, connection, uninterruptible: true);

		try
		{
			await Execute($"EXEC {Quote(staging)}.sys.sp_addextendedproperty @name = @marker, @value = N'true'", staging, connection, uninterruptible: true);
			await Execute($"ALTER DATABASE {Quote(staging)} MODIFY NAME = {Quote(name)}", staging, connection, uninterruptible: true);
		}
		catch (SqlException ex)
		{
			/*
			 * On a fresh connection, because the failure may have been the connection's own.
			 */
			try
			{
				await using var cleanup = await Open(CancellationToken.None);

				await Execute($"DROP DATABASE IF EXISTS {Quote(staging)}", staging, cleanup, uninterruptible: true);
			}
			catch (SqlException)
			{
				/*
				 * The original failure is the one worth reporting; a staging database left behind holds no name an
				 * application uses.
				 */
			}

			if (ex.Number != DatabaseExists)
				throw;

			if (await Probe(name, connection) != true)
				throw NotAContainer(name);
		}
	}

	/// <returns><c>null</c> if no database has the name, otherwise whether it is a container.</returns>
	/// <exception cref="InvalidOperationException">The database is not online.</exception>
	/// <remarks>
	/// A database that is not online has no readable marker. A container is never left in such a state under its own
	/// name, so one that is may belong to someone else and is not touched.
	/// </remarks>
	private async Task<bool?> Probe(string name, SqlConnection connection)
	{
		if (await Scalar("SELECT state_desc FROM sys.databases WHERE name = @name", name, connection) is not string state)
			return null;

		if (!string.Equals(state, "ONLINE", StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException($"The storage '{name}' is {state}, so whether it is a storage container cannot be read; it is left as it is.");

		return await Scalar($"SELECT 1 FROM {Quote(name)}.sys.extended_properties WHERE class = 0 AND name = @marker", name, connection) is not (null or DBNull);
	}

	/// <summary>
	/// Removes databases left behind by interrupted drops. Each is best effort: another drop may be removing the same
	/// database, and a database that cannot be removed now is tried again on the next drop.
	/// </summary>
	private async Task DropAbandoned(SqlConnection connection)
	{
		var abandoned = new List<(string Name, string State)>();

		await using (var command = CreateCommand("SELECT name, state_desc FROM sys.databases WHERE name LIKE @prefix", string.Empty, connection))
		{
			command.Parameters.AddWithValue("@prefix", $"{DroppingPrefix.Replace("_", "[_]")}%");

			await using var reader = await command.ExecuteReaderAsync(CancellationToken);

			while (await reader.ReadAsync(CancellationToken))
				abandoned.Add((reader.GetString(0), reader.GetString(1)));
		}

		foreach (var (name, state) in abandoned)
		{
			try
			{
				await Remove(name, state, connection);
			}
			catch (SqlException)
			{
			}
		}
	}

	/// <summary>
	/// Drops a database only this provider names. An offline database is brought online first, because dropping it
	/// offline would leave its files on disk.
	/// </summary>
	private async Task Remove(string name, string state, SqlConnection connection)
	{
		if (string.Equals(state, "OFFLINE", StringComparison.OrdinalIgnoreCase))
			await Execute($"ALTER DATABASE {Quote(name)} SET ONLINE", name, connection, uninterruptible: true);

		if (string.Equals(state, "ONLINE", StringComparison.OrdinalIgnoreCase) || string.Equals(state, "OFFLINE", StringComparison.OrdinalIgnoreCase))
			await Execute($"ALTER DATABASE {Quote(name)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE", name, connection, uninterruptible: true);

		await Execute($"DROP DATABASE {Quote(name)}", name, connection, uninterruptible: true);
	}

	/// <summary>
	/// Refuses Azure SQL Database, which supports neither the cross-database references that mark a container nor
	/// renaming a database from another one.
	/// </summary>
	/// <exception cref="NotSupportedException">The server is Azure SQL Database.</exception>
	private async Task EnsureSupported(SqlConnection connection)
	{
		_supported ??= Convert.ToInt32(await Scalar("SELECT SERVERPROPERTY('EngineEdition')", string.Empty, connection)) != AzureSqlDatabase;

		if (_supported == false)
			throw new NotSupportedException("Storage containers are not supported on Azure SQL Database.");
	}

	/// <param name="uninterruptible">Runs without a timeout and ignores cancellation, for steps that must not stop midway.</param>
	private async Task Execute(string text, string name, SqlConnection connection, bool uninterruptible = false)
	{
		await using var command = CreateCommand(text, name, connection);

		if (uninterruptible)
			command.CommandTimeout = 0;

		await command.ExecuteNonQueryAsync(uninterruptible ? CancellationToken.None : CancellationToken);
	}

	private async Task<object?> Scalar(string text, string name, SqlConnection connection)
	{
		await using var command = CreateCommand(text, name, connection);

		return await command.ExecuteScalarAsync(CancellationToken);
	}

	private Task<SqlConnection> Open() => Open(CancellationToken);

	private async Task<SqlConnection> Open(CancellationToken cancel)
	{
		var connection = new SqlConnection(new SqlConnectionStringBuilder(DefaultConnectionString) { InitialCatalog = "master" }.ConnectionString);

		await connection.OpenAsync(cancel);

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
