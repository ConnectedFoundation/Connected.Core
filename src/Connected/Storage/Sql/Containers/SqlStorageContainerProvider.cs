using Connected.Configuration;
using Connected.Storage.Containers;
using Microsoft.Data.SqlClient;
using System.Globalization;

namespace Connected.Storage.Sql.Containers;

/// <summary>
/// Manages SQL Server storage containers: each container is a database on the default database's server, marked with
/// a database-level extended property.
/// </summary>
/// <remarks>
/// Database names starting with <c>connected_staging_</c> and <c>connected_dropping_</c> are the provider's own: a
/// container is created under the first and renamed when it is complete, and renamed to the second before it is dropped.
/// The provider changes the state of no database that is not online or does not carry the container marker.
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
	/// The error raised when a single-user database's one connection is taken.
	/// </summary>
	private const int SingleUserInUse = 924;

	private const int DropAttempts = 3;

	private const string StagingPrefix = "connected_staging_";
	private const string DroppingPrefix = "connected_dropping_";
	private const string TimeFormat = "yyyyMMddHHmmss";

	/// <summary>
	/// How long after its create or drop started a database under <see cref="StagingPrefix"/> or
	/// <see cref="DroppingPrefix"/> counts as abandoned; well beyond what any create or drop takes, so a later drop never
	/// removes one a concurrent create or drop is still working on.
	/// </summary>
	private static readonly TimeSpan AbandonedAfter = TimeSpan.FromHours(1);

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

		bool? container;

		try
		{
			container = await Probe(name, connection);
		}
		catch (SqlException ex) when (ex.Number == SingleUserInUse)
		{
			throw InUse(name, ex);
		}

		switch (container)
		{
			case null:
				await CreateMarked(name, connection);
				break;
			case false:
				throw NotAContainer(name);
			case true:
				/*
				 * A drop interrupted before its rename can leave the container single user. Its one connection was free
				 * for the probe, and the container is now known to be one, so a client that took it since is rolled
				 * back.
				 */
				if (await Scalar("SELECT user_access_desc FROM sys.databases WHERE name = @name", name, connection) is string access
					&& !string.Equals(access, "MULTI_USER", StringComparison.OrdinalIgnoreCase))
					await Execute($"ALTER DATABASE {Quote(name)} SET MULTI_USER WITH ROLLBACK IMMEDIATE", name, connection, uninterruptible: true);
				break;
		}

		return new SqlConnectionStringBuilder(DefaultConnectionString) { InitialCatalog = name }.ConnectionString;
	}

	/// <inheritdoc/>
	protected override async Task OnDrop(string name)
	{
		var connection = await Open();

		try
		{
			await EnsureSupported(connection);
			await DropAbandoned(connection);

			SqlConnection.ClearPool(new SqlConnection(new SqlConnectionStringBuilder(DefaultConnectionString) { InitialCatalog = name }.ConnectionString));

			for (var attempt = 1; ; attempt++)
			{
				string dropping;

				try
				{
					switch (await Probe(name, connection))
					{
						case null:
							/*
							 * Also where a concurrent drop of the same container got there first.
							 */
							return;
						case false:
							throw NotAContainer(name);
					}

					dropping = await Rename(name, connection);
				}
				catch (SqlException) when (attempt < DropAttempts)
				{
					/*
					 * Another session holds the container's single connection (a concurrent drop), or rolled this one
					 * back. The next attempt starts on a fresh connection and finds the container gone if the other
					 * drop finished.
					 */
					await Release(name, connection);
					await connection.DisposeAsync();
					await Task.Delay(TimeSpan.FromSeconds(attempt), CancellationToken);

					connection = await Open();

					continue;
				}
				catch (SqlException ex)
				{
					await Release(name, connection);

					if (ex.Number == SingleUserInUse)
						throw InUse(name, ex);

					throw;
				}
				/*
				 * Once renamed, clients reconnecting by the old name fail, so nothing takes the database's single
				 * connection before it is dropped from master.
				 */
				await Execute($"""
					USE master;
					DROP DATABASE IF EXISTS {Quote(dropping)};
					""", dropping, connection, uninterruptible: true);

				return;
			}
		}
		finally
		{
			await connection.DisposeAsync();
		}
	}

	/// <summary>
	/// Renames the container to a name only this provider uses.
	/// </summary>
	/// <remarks>
	/// The session switches to the container before taking it to single user, so it keeps the one connection allowed
	/// and no client, the application's own login included, can take it before the rename. The steps run without a
	/// timeout and are not cancelled midway; a drop interrupted anyway leaves either the container under its own name,
	/// still marked, or a database only this provider names, which a later drop removes once it is old enough.
	/// </remarks>
	/// <returns>The database's new name.</returns>
	private async Task<string> Rename(string name, SqlConnection connection)
	{
		var dropping = ProviderName(DroppingPrefix);

		await Execute($"""
			USE {Quote(name)};
			ALTER DATABASE {Quote(name)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
			ALTER DATABASE {Quote(name)} MODIFY NAME = {Quote(dropping)};
			""", name, connection, uninterruptible: true);

		return dropping;
	}

	/// <summary>
	/// Opens the container to every user again if this session holds its single connection, after a drop that failed
	/// before the rename. A session that never got into the container leaves it alone: whoever holds it is not this
	/// drop's to undo.
	/// </summary>
	private async Task Release(string name, SqlConnection connection)
	{
		if (connection.State != System.Data.ConnectionState.Open)
			return;

		try
		{
			if (!string.Equals(await Scalar("SELECT DB_NAME()", name, connection, uninterruptible: true) as string, name, StringComparison.OrdinalIgnoreCase))
				return;

			await Execute($"""
				ALTER DATABASE {Quote(name)} SET MULTI_USER;
				USE master;
				""", name, connection, uninterruptible: true);
		}
		catch (SqlException)
		{
			/*
			 * The session ends with the connection, which frees the single connection; the container stays single
			 * user until the next create opens it again.
			 */
		}
	}

	/// <summary>
	/// Returns the exception thrown for a database in single-user mode whose one connection another session holds: its
	/// marker cannot be read, so it is left as it is.
	/// </summary>
	private static InvalidOperationException InUse(string name, SqlException inner)
		=> new($"The storage '{name}' is in single-user mode and its one connection is in use, so whether it is a storage container cannot be read; it is left as it is.", inner);

	/// <summary>
	/// A name only this provider uses, carrying the time the create or drop started, so a later drop can tell an
	/// abandoned database from one a concurrent create or drop is still working on.
	/// </summary>
	private static string ProviderName(string prefix) => $"{prefix}{DateTimeOffset.UtcNow.ToString(TimeFormat, CultureInfo.InvariantCulture)}_{Guid.NewGuid():N}";

	/// <summary>
	/// Creates and marks the database under a staging name and only then renames it, so the container's name never
	/// belongs to an unmarked database. A failure drops the staging database; two concurrent creates race on the
	/// rename, and the one that loses finds the other's container.
	/// </summary>
	private async Task CreateMarked(string name, SqlConnection connection)
	{
		var staging = ProviderName(StagingPrefix);

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
	/// Removes databases left behind by interrupted creates and drops: only online ones that carry the container marker
	/// and whose create or drop started long enough ago that nothing can still be working on them. Each is best effort,
	/// and one that cannot be removed now is tried again by a later drop. A staging database interrupted before it was
	/// marked is not removed, like any other unmarked database.
	/// </summary>
	private async Task DropAbandoned(SqlConnection connection)
	{
		var candidates = new List<string>();

		await using (var command = CreateCommand("SELECT name FROM sys.databases WHERE (name LIKE @staging OR name LIKE @dropping) AND state_desc = 'ONLINE'", string.Empty, connection))
		{
			command.Parameters.AddWithValue("@staging", $"{StagingPrefix.Replace("_", "[_]")}%");
			command.Parameters.AddWithValue("@dropping", $"{DroppingPrefix.Replace("_", "[_]")}%");

			await using var reader = await command.ExecuteReaderAsync(CancellationToken);

			while (await reader.ReadAsync(CancellationToken))
				candidates.Add(reader.GetString(0));
		}

		foreach (var name in candidates)
		{
			if (Started(name) is not { } started || DateTimeOffset.UtcNow - started < AbandonedAfter)
				continue;

			try
			{
				if (await Probe(name, connection) != true)
					continue;

				await Execute($"""
					ALTER DATABASE {Quote(name)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
					DROP DATABASE IF EXISTS {Quote(name)};
					""", name, connection, uninterruptible: true);
			}
			catch (Exception ex) when (ex is SqlException or InvalidOperationException)
			{
			}
		}
	}

	/// <returns>When the create or drop that named the database started, or <c>null</c> for a name this provider did not write.</returns>
	private static DateTimeOffset? Started(string name)
	{
		var prefix = name.StartsWith(StagingPrefix, StringComparison.Ordinal) ? StagingPrefix : DroppingPrefix;
		var stamp = name.Length >= prefix.Length + TimeFormat.Length
			? name.Substring(prefix.Length, TimeFormat.Length)
			: string.Empty;

		return DateTimeOffset.TryParseExact(stamp, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var started)
			? started
			: null;
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

	private async Task<object?> Scalar(string text, string name, SqlConnection connection, bool uninterruptible = false)
	{
		await using var command = CreateCommand(text, name, connection);

		if (uninterruptible)
			command.CommandTimeout = 0;

		return await command.ExecuteScalarAsync(uninterruptible ? CancellationToken.None : CancellationToken);
	}

	private Task<SqlConnection> Open() => Open(CancellationToken);

	private async Task<SqlConnection> Open(CancellationToken cancel)
	{
		/*
		 * Not pooled: a session that switched to a container must end with the connection, or a pooled connection would
		 * keep holding the container's single connection.
		 */
		var connection = new SqlConnection(new SqlConnectionStringBuilder(DefaultConnectionString) { InitialCatalog = "master", Pooling = false }.ConnectionString);

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
