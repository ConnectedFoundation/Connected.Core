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

	/// <summary>
	/// The error a create or rename raises when a database with the name exists.
	/// </summary>
	private const int DatabaseExists = 1801;

	/// <summary>
	/// The engine edition <c>SERVERPROPERTY('EngineEdition')</c> reports for Azure SQL Database.
	/// </summary>
	private const int AzureSqlDatabase = 5;

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
		await EnsureSupported();

		switch (await Probe(name))
		{
			case null:
				await CreateMarked(name);
				break;
			case false:
				throw NotAContainer(name);
		}

		return new SqlConnectionStringBuilder(DefaultConnectionString) { InitialCatalog = name }.ConnectionString;
	}

	/// <inheritdoc/>
	protected override async Task OnDrop(string name)
	{
		await EnsureSupported();

		switch (await Probe(name))
		{
			case null:
				return;
			case false:
				throw NotAContainer(name);
		}

		/*
		 * Pooled connections would keep the database in use; open sessions are rolled back so the drop cannot wait.
		 * Taking the database offline rather than to single user means a client reconnecting before the drop can only
		 * make the drop fail, never leave the database stuck in single user mode. Rollback and recovery can take long,
		 * so the batch runs without a timeout and is not cancelled midway; a drop interrupted anyway leaves the
		 * database offline, which the next probe recovers from.
		 */
		SqlConnection.ClearPool(new SqlConnection(new SqlConnectionStringBuilder(DefaultConnectionString) { InitialCatalog = name }.ConnectionString));

		await Execute($"""
			ALTER DATABASE {Quote(name)} SET OFFLINE WITH ROLLBACK IMMEDIATE;
			ALTER DATABASE {Quote(name)} SET ONLINE;
			DROP DATABASE {Quote(name)};
			""", name, uninterruptible: true);
	}

	/// <summary>
	/// Creates and marks the database under a staging name and only then renames it, so the container's name never
	/// belongs to an unmarked database. A failure drops the staging database; two concurrent creates race on the
	/// rename, and the one that loses finds the other's container.
	/// </summary>
	private async Task CreateMarked(string name)
	{
		var staging = $"connected_staging_{Guid.NewGuid():N}";

		await Execute($"CREATE DATABASE {Quote(staging)}", staging, uninterruptible: true);

		try
		{
			await Execute($"EXEC {Quote(staging)}.sys.sp_addextendedproperty @name = @marker, @value = N'true'", staging, uninterruptible: true);
			await Execute($"ALTER DATABASE {Quote(staging)} MODIFY NAME = {Quote(name)}", staging, uninterruptible: true);
		}
		catch (SqlException ex)
		{
			try
			{
				await Execute($"DROP DATABASE {Quote(staging)}", staging, uninterruptible: true);
			}
			catch (SqlException)
			{
				/*
				 * The original failure is the one worth reporting; a staging database left behind is marked and
				 * holds no name an application uses.
				 */
			}

			if (ex.Number != DatabaseExists)
				throw;

			if (await Probe(name) != true)
				throw NotAContainer(name);
		}
	}

	/// <returns><c>null</c> if no database has the name, otherwise whether it is a container.</returns>
	/// <remarks>
	/// A database left offline by an interrupted drop has no readable marker, so it is brought online to be checked
	/// and taken offline again if it is not a container.
	/// </remarks>
	private async Task<bool?> Probe(string name)
	{
		if (await Scalar("SELECT state_desc FROM sys.databases WHERE name = @name", name) is not string state)
			return null;

		if (!string.Equals(state, "OFFLINE", StringComparison.OrdinalIgnoreCase))
			return await IsContainer(name);

		await Execute($"ALTER DATABASE {Quote(name)} SET ONLINE", name, uninterruptible: true);

		if (await IsContainer(name))
			return true;

		await Execute($"ALTER DATABASE {Quote(name)} SET OFFLINE", name, uninterruptible: true);

		return false;
	}

	private async Task<bool> IsContainer(string name)
	{
		return await Scalar($"SELECT 1 FROM {Quote(name)}.sys.extended_properties WHERE class = 0 AND name = @marker", name) is not (null or DBNull);
	}

	/// <summary>
	/// Refuses Azure SQL Database, which supports neither the cross-database references that mark a container nor
	/// taking a database offline to drop it.
	/// </summary>
	/// <exception cref="NotSupportedException">The server is Azure SQL Database.</exception>
	private async Task EnsureSupported()
	{
		if (Convert.ToInt32(await Scalar("SELECT SERVERPROPERTY('EngineEdition')", string.Empty)) == AzureSqlDatabase)
			throw new NotSupportedException("Storage containers are not supported on Azure SQL Database.");
	}

	/// <param name="uninterruptible">Runs without a timeout and ignores cancellation, for steps that must not stop midway.</param>
	private async Task Execute(string text, string name, bool uninterruptible = false)
	{
		var cancel = uninterruptible ? CancellationToken.None : CancellationToken;

		await using var connection = await Open(cancel);
		await using var command = CreateCommand(text, name, connection);

		if (uninterruptible)
			command.CommandTimeout = 0;

		await command.ExecuteNonQueryAsync(cancel);
	}

	private async Task<object?> Scalar(string text, string name)
	{
		await using var connection = await Open(CancellationToken);
		await using var command = CreateCommand(text, name, connection);

		return await command.ExecuteScalarAsync(CancellationToken);
	}

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
