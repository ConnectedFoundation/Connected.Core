using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;
using Connected.Storage.Sql;
using Connected.Storage.Sql.Transactions;
using Microsoft.Data.SqlClient;

namespace Connected.Storage.Tests;

/// <summary>
/// The version of a <see cref="ConsistentEntity{TPrimaryKey}"/> on a real SQL Server (<c>CONNECTED_TEST_SQLSERVER</c>): the commands the
/// builders write, run through the storage connection, name the version, hand back the one they produced, and refuse a stale one.
/// </summary>
[TestClass]
public sealed class SqlVersionLiveTests
{
	private const string Variable = "CONNECTED_TEST_SQLSERVER";
	private const string Table = "versionLiveRow";

	[Table("dbo")]
	internal sealed record VersionLiveRow : ConsistentEntity<int>
	{
		[Ordinal(0), Length(32)]
		public string? First { get; init; }
	}

	private sealed class OperationCommand(IStorageConnection connection, SqlStorageOperation operation) : IStorageCommand
	{
		public IStorageOperation Operation { get; } = operation;
		public IStorageConnection Connection { get; } = connection;

		public void Dispose()
		{
		}

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	/// <summary>Runs one command the way the storage does and binds what it handed back to a copy of the entity.</summary>
	private static async Task<(VersionLiveRow Entity, int Affected, SqlStorageOperation Operation)> Run(SqlDataConnection connection, VersionLiveRow entity)
	{
		var operation = entity.State switch
		{
			State.Add => await new InsertCommandBuilder<VersionLiveRow>(null!).Build(entity, null, CancellationToken.None),
			_ => await new UpdateCommandBuilder<VersionLiveRow>(null!).Build(entity, null, CancellationToken.None)
		};

		Assert.IsNotNull(operation);

		var affected = await connection.Execute(new OperationCommand(connection, operation));
		var bound = entity with { };

		ReturnValueBinder.Bind(operation, bound);

		return (bound, affected, operation);
	}

	[TestMethod]
	public async Task TheVersionIsNamedHandedBackAndRefusedWhenStale()
	{
		await using var setup = new SqlConnection(TestConfiguration.From(Variable));

		await setup.OpenAsync();

		// The builders write to the table the entity's schema names.
		var name = $"dbo.[{Table}]";

		await using (var drop = new SqlCommand($"DROP TABLE IF EXISTS {name}", setup))
			await drop.ExecuteNonQueryAsync();

		await using (var create = new SqlCommand($"CREATE TABLE {name} ([id] int IDENTITY PRIMARY KEY, [first] varchar(32) NULL, [eTag] timestamp)", setup))
			await create.ExecuteNonQueryAsync();

		try
		{
			// Disposed before the table is dropped, whatever happens in between: an open transaction would hold the table's locks.
			await using var connection = new SqlDataConnection(null!);

			await connection.Initialize(new TestConnectionDto { ConnectionString = TestConfiguration.From(Variable) });

			// An insert hands back the id and the version the database gave the row.
			var inserted = await Run(connection, new VersionLiveRow { State = State.Add, First = "a" });

			Assert.AreEqual(1, inserted.Affected);
			Assert.AreNotEqual(0, inserted.Entity.Id);
			Assert.IsFalse(string.IsNullOrEmpty(inserted.Entity.ETag), "the insert returned no version");

			// The entity it returned is written again without being read: the version it carries is the one stored.
			var first = await Run(connection, inserted.Entity with { State = State.Update, First = "b" });

			Assert.AreEqual(1, first.Affected, "the update of the entity the insert returned was refused");
			Assert.AreEqual(DataConcurrencyMode.Enabled, first.Operation.Concurrency);
			Assert.AreNotEqual(inserted.Entity.ETag, first.Entity.ETag, "the update returned the version it replaced");

			var second = await Run(connection, first.Entity with { State = State.Update, First = "c" });

			Assert.AreEqual(1, second.Affected, "the update of the entity the update returned was refused");
			Assert.AreNotEqual(first.Entity.ETag, second.Entity.ETag);

			// A writer that still holds an older version is refused: no row is changed.
			var stale = await Run(connection, inserted.Entity with { State = State.Update, First = "stale" });

			Assert.AreEqual(0, stale.Affected);
			Assert.AreEqual(DataConcurrencyMode.Enabled, stale.Operation.Concurrency);

			await using var read = new SqlCommand($"SELECT [first] FROM {name} WITH (NOLOCK) WHERE [id] = {inserted.Entity.Id}", setup);

			Assert.AreEqual("c", (string?)await read.ExecuteScalarAsync());

			// An entity that carries no version is updated as before.
			var unversioned = await Run(connection, new VersionLiveRow { State = State.Update, Id = inserted.Entity.Id, First = "d" });

			Assert.AreEqual(1, unversioned.Affected);
			Assert.AreEqual(DataConcurrencyMode.Disabled, unversioned.Operation.Concurrency);
			Assert.IsFalse(string.IsNullOrEmpty(unversioned.Entity.ETag), "the update of an entity without a version returned none");

			// Written again with the version it was just given: no conflict.
			var again = await Run(connection, unversioned.Entity with { State = State.Update, First = "e" });

			Assert.AreEqual(1, again.Affected);

			await connection.Commit();
		}
		finally
		{
			await using var cleanup = new SqlCommand($"DROP TABLE IF EXISTS {name}", setup);

			await cleanup.ExecuteNonQueryAsync();
		}
	}
}
