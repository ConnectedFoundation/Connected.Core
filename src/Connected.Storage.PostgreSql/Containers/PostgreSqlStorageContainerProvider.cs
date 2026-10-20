using Connected.Configuration;
using Connected.Storage.Containers;
using Npgsql;
using System.Globalization;

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

	private const string StagingPrefix = "connected_staging_";
	private const string TimeFormat = "yyyyMMddHHmmss";

	/// <summary>
	/// How long after its create started a staging database counts as abandoned; well beyond what any create takes, so
	/// a drop never removes one a concurrent create is still working on.
	/// </summary>
	private static readonly TimeSpan AbandonedAfter = TimeSpan.FromHours(1);

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

		await DropAbandoned(connection);

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

		await Execute($"DROP DATABASE IF EXISTS {Quote(name)} WITH (FORCE)", connection, CancellationToken);
	}

	/// <summary>
	/// Creates and marks the database under a staging name and only then renames it, so the container's name never
	/// belongs to an unmarked database. A failure drops the staging database; two concurrent creates race on the
	/// rename, and the one that loses finds the other's container. The steps are not cancelled midway.
	/// </summary>
	private async Task CreateMarked(string name, NpgsqlConnection connection)
	{
		/*
		 * The time the create started, so a later drop can tell an abandoned staging database from one a concurrent
		 * create is still working on; the id is shortened to keep the name within the 63-byte identifier limit.
		 */
		var staging = $"{StagingPrefix}{DateTimeOffset.UtcNow.ToString(TimeFormat, CultureInfo.InvariantCulture)}_{Guid.NewGuid().ToString("N")[..16]}";

		await Execute($"CREATE DATABASE {Quote(staging)}", connection, CancellationToken.None);

		try
		{
			await Execute($"COMMENT ON DATABASE {Quote(staging)} IS '{ContainerMarker}'", connection, CancellationToken.None);
			await Execute($"ALTER DATABASE {Quote(staging)} RENAME TO {Quote(name)}", connection, CancellationToken.None);
		}
		catch (NpgsqlException ex)
		{
			/*
			 * On a fresh connection, because the failure may have been the connection's own.
			 */
			try
			{
				await using var cleanup = await Open(CancellationToken.None);

				await Execute($"DROP DATABASE IF EXISTS {Quote(staging)}", cleanup, CancellationToken.None);
			}
			catch (NpgsqlException)
			{
				/*
				 * The original failure is the one worth reporting; a staging database left behind holds no name an
				 * application uses.
				 */
			}

			if (ex is not PostgresException { SqlState: PostgresErrorCodes.DuplicateDatabase })
				throw;

			if (await Probe(name, connection) != true)
				throw NotAContainer(name);
		}
	}

	/// <summary>
	/// Removes staging databases left behind by interrupted creates: only ones that carry the container marker and whose
	/// create started long enough ago that no create can still be working on them. Each is best effort, and one that
	/// cannot be removed now is tried again by a later drop. A staging database interrupted before it was marked is not
	/// removed, like any other unmarked database.
	/// </summary>
	private async Task DropAbandoned(NpgsqlConnection connection)
	{
		var candidates = new List<string>();

		await using (var command = new NpgsqlCommand("SELECT datname FROM pg_database WHERE datname LIKE @prefix AND shobj_description(oid, 'pg_database') = @marker", connection))
		{
			command.Parameters.AddWithValue("prefix", $"{StagingPrefix.Replace("_", "\\_")}%");
			command.Parameters.AddWithValue("marker", ContainerMarker);

			await using var reader = await command.ExecuteReaderAsync(CancellationToken);

			while (await reader.ReadAsync(CancellationToken))
				candidates.Add(reader.GetString(0));
		}

		foreach (var name in candidates)
		{
			var stamp = name.Length >= StagingPrefix.Length + TimeFormat.Length ? name.Substring(StagingPrefix.Length, TimeFormat.Length) : string.Empty;

			if (!DateTimeOffset.TryParseExact(stamp, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var started)
				|| DateTimeOffset.UtcNow - started < AbandonedAfter)
				continue;

			try
			{
				await Execute($"DROP DATABASE IF EXISTS {Quote(name)} WITH (FORCE)", connection, CancellationToken.None);
			}
			catch (NpgsqlException)
			{
			}
		}
	}

	/// <summary>
	/// Runs a database command without a timeout: creating from a large template or dropping a large database can take
	/// longer than the default.
	/// </summary>
	private static async Task Execute(string text, NpgsqlConnection connection, CancellationToken cancel)
	{
		await using var command = new NpgsqlCommand(text, connection) { CommandTimeout = 0 };

		await command.ExecuteNonQueryAsync(cancel);
	}

	/// <returns><c>null</c> if no database has the name, otherwise whether it is a container.</returns>
	private async Task<bool?> Probe(string name, NpgsqlConnection connection)
	{
		await using var command = new NpgsqlCommand("SELECT coalesce(shobj_description(oid, 'pg_database'), '') FROM pg_database WHERE datname = @name", connection);

		command.Parameters.AddWithValue("name", name);

		return await command.ExecuteScalarAsync(CancellationToken) is string comment ? comment == ContainerMarker : null;
	}

	private Task<NpgsqlConnection> Open() => Open(CancellationToken);

	private async Task<NpgsqlConnection> Open(CancellationToken cancel)
	{
		var connection = new NpgsqlConnection(DefaultConnectionString);

		await connection.OpenAsync(cancel);

		return connection;
	}

	private static string Quote(string name) => $"\"{name.Replace("\"", "\"\"")}\"";
}
