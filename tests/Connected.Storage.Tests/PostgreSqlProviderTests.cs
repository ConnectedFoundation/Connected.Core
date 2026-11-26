using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;
using Connected.Data.Expressions;
using Connected.Data.Expressions.Expressions;
using Connected.Data.Expressions.Translation;
using Connected.Storage.PostgreSql;
using Connected.Storage.PostgreSql.Query;
using Connected.Storage.PostgreSql.Schemas;
using Connected.Storage.PostgreSql.Transactions;
using Connected.Storage.Schemas;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Connected.Storage.Tests;

/// <summary>
/// Covers failures found running a real application on the PostgreSQL provider.
/// </summary>
/// <remarks>
/// The tests that need a server read it from <c>CONNECTED_TEST_POSTGRES</c>.
/// </remarks>
[TestClass]
public sealed class PostgreSqlProviderTests
{
	private const string Variable = "CONNECTED_TEST_POSTGRES";

	internal enum Kind { Basic = 1, Fancy = 2 }

	[Table("test")]
	internal sealed record UnnamedRow : ConsistentEntity<int>
	{
		[Ordinal(0), Length(32)]
		public string? First { get; init; }

		[Ordinal(1)]
		public int Second { get; init; }
	}

	private static UnnamedRow Row(State state) => new() { State = state, Id = 5, First = "a", Second = 2, ETag = "00000000000000ff" };

	private static async Task<PostgreSqlStorageOperation> Build(State state, IEnumerable<string>? properties = null)
	{
		var operation = await AggregatedCommandBuilder<UnnamedRow>.Build(Row(state), properties, CancellationToken.None);

		Assert.IsNotNull(operation);

		return operation;
	}

	[TestMethod]
	public void AnEnumIsWrittenAsItsNumber()
	{
		Assert.AreEqual(2, PostgreSqlStorageConnection.ToParameterValue(Kind.Fancy));
	}

	[TestMethod]
	public void NullIsWrittenAsDbNull()
	{
		Assert.AreEqual(DBNull.Value, PostgreSqlStorageConnection.ToParameterValue(null));
	}

	[TestMethod]
	[DataRow(State.Add)]
	[DataRow(State.Update)]
	[DataRow(State.Delete)]
	public async Task WritesGoToTheTableSynchronizationCreates(State state)
	{
		StringAssert.Contains((await Build(state)).CommandText, "\"test\".\"unnamedRow\"");
	}

	[TestMethod]
	public async Task AnUpdateChecksTheVersionAndWritesANewOne()
	{
		var text = (await Build(State.Update)).CommandText;

		StringAssert.Contains(text, $"\"eTag\" = {PostgreSqlVersion.Next}");
		StringAssert.Contains(text[text.IndexOf("WHERE", StringComparison.Ordinal)..], "\"eTag\" = @eTag");
	}

	[TestMethod]
	public async Task AnUpdateEnablesConcurrency()
	{
		Assert.AreEqual(DataConcurrencyMode.Enabled, (await Build(State.Update)).Concurrency);
	}

	[TestMethod]
	public async Task ALimitedUpdateWritesOnlyTheNamedProperty()
	{
		var text = (await Build(State.Update, [nameof(UnnamedRow.Second)])).CommandText;

		StringAssert.Contains(text, "\"second\" = @second");
		Assert.IsFalse(text.Contains("\"first\" ="), text);
	}

	[TestMethod]
	public async Task LimitedAndFullUpdatesDoNotShareACachedCommand()
	{
		await Build(State.Update, [nameof(UnnamedRow.Second)]);
		var full = await Build(State.Update);

		StringAssert.Contains(full.CommandText, "\"first\" = @first");
	}

	[TestMethod]
	public async Task ACachedUpdateCarriesTheVersionOfItsOwnEntity()
	{
		await Build(State.Update);

		var again = await Build(State.Update);
		var version = again.Parameters.Single(f => f.Name == "@eTag");

		CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0xff }, (byte[])version.Value!);
	}

	[TestMethod]
	public async Task AnInsertLeavesTheVersionToTheColumnDefault()
	{
		var text = (await Build(State.Add)).CommandText;

		Assert.IsFalse(text[..text.IndexOf("VALUES", StringComparison.Ordinal)].Contains("eTag"), text);
		Assert.IsFalse(text[text.IndexOf("VALUES", StringComparison.Ordinal)..text.IndexOf("RETURNING", StringComparison.Ordinal)].Contains("eTag"), text);
	}

	[TestMethod]
	public async Task AnInsertReturnsTheKeyAndTheVersionTheColumnDefaultGave()
	{
		var operation = await Build(State.Add);

		StringAssert.Contains(operation.CommandText, "RETURNING \"id\", \"eTag\"");
		CollectionAssert.AreEqual(new[] { "@id", "@eTag" }, operation.Parameters.Where(f => f.Direction == ParameterDirection.Output).Select(f => f.Name).ToArray());
	}

	[TestMethod]
	public async Task AnUpdateReturnsTheVersionItWroteThroughTheParameterThatCarriedTheOldOne()
	{
		var operation = await Build(State.Update);

		StringAssert.Contains(operation.CommandText, "RETURNING \"eTag\"");
		Assert.AreEqual(ParameterDirection.InputOutput, operation.Parameters.Single(f => f.Name == "@eTag").Direction);
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	public async Task AnUpdateOfAnEntityWithoutAVersionIsNotConditionalOnItButReturnsTheOneItWrote(string? eTag)
	{
		var operation = await AggregatedCommandBuilder<UnnamedRow>.Build(Row(State.Update) with { ETag = eTag }, null, CancellationToken.None);

		Assert.IsNotNull(operation);

		var where = operation.CommandText[operation.CommandText.IndexOf("WHERE", StringComparison.Ordinal)..];

		Assert.IsFalse(where.Substring(0, where.IndexOf("RETURNING", StringComparison.Ordinal)).Contains("eTag"), where);
		StringAssert.Contains(operation.CommandText, "RETURNING \"eTag\"");
		Assert.AreEqual(DataConcurrencyMode.Disabled, operation.Concurrency);
		Assert.AreEqual(ParameterDirection.Output, operation.Parameters.Single(f => f.Name == "@eTag").Direction);
	}

	[TestMethod]
	public async Task AnEntityWithoutAVersionDoesNotTakeTheCommandOfOneThatHasIt()
	{
		await Build(State.Update);

		var unversioned = await AggregatedCommandBuilder<UnnamedRow>.Build(Row(State.Update) with { ETag = null }, null, CancellationToken.None);

		Assert.AreEqual(DataConcurrencyMode.Disabled, unversioned!.Concurrency);
		Assert.AreEqual(DataConcurrencyMode.Enabled, (await Build(State.Update)).Concurrency);
	}

	[TestMethod]
	public void AVersionColumnGetsADefault()
	{
		StringAssert.Contains(Ddl.Column(new Column { Name = "eTag", DataType = DbType.Binary, IsVersion = true }), $"DEFAULT {PostgreSqlVersion.Next}");
	}

	/*
	 * bytea has no length: PostgreSQL refuses "bytea(n)" with "type modifier is not allowed for type bytea". An entity derived from
	 * QueueMessage (the mail queue of Connected.ServiceModel) carries a binary column with a length, and the schema of any
	 * application that loads it could not be synchronised at all.
	 */
	[TestMethod]
	public void ABinaryColumnWithALengthIsCreatedWithoutOne()
	{
		var text = Ddl.Column(new Column { Name = "payload", DataType = DbType.Binary, MaxLength = 8 });

		StringAssert.Contains(text, "BYTEA");
		Assert.IsFalse(text.Contains("BYTEA(", StringComparison.OrdinalIgnoreCase), text);
	}

	/*
	 * information_schema has no length for bytea (it reads as 0). A binary column whose property declares a length must still be
	 * the same column as the one in the database, or every start alters it again.
	 */
	[TestMethod]
	public void ABinaryColumnIsNotAlteredForItsLengthOnEveryStart()
	{
		var desired = new Column { Name = "payload", DataType = DbType.Binary, MaxLength = 8 };
		var existing = new Column { Name = "payload", DataType = DbType.Binary, MaxLength = 0 };

		Assert.IsTrue(ColumnComparer.Equals(desired, existing));
	}

	/*
	 * The two halves together against a server: it accepts the column the DDL writes for a binary property with a length, and what
	 * the catalog reports for it is the same column as the property's.
	 */
	[TestMethod]
	public async Task ABinaryColumnWithALengthIsCreatedByTheServerAndReadBackAsTheSameColumn()
	{
		await using var connection = await Open();

		var desired = new Column { Name = "payload", DataType = DbType.Binary, MaxLength = 8 };

		await Execute(connection, $"CREATE TEMP TABLE \"binaryProbe\" ({Ddl.Column(desired)})");

		await using var command = new NpgsqlCommand("SELECT data_type, character_maximum_length FROM information_schema.columns WHERE table_name = 'binaryProbe' AND column_name = 'payload'", connection);
		await using var reader = await command.ExecuteReaderAsync();

		Assert.IsTrue(await reader.ReadAsync());
		Assert.AreEqual("bytea", reader.GetString(0));

		var existing = new Column { Name = "payload", DataType = DbType.Binary, MaxLength = reader.IsDBNull(1) ? 0 : reader.GetInt32(1) };

		Assert.IsTrue(ColumnComparer.Equals(desired, existing));
	}

	/*
	 * The shared cache keeps an entity only if its version is not lower than the cached one (Entry.Set), as a rowversion
	 * only grows. Random versions were lower half the time, so half of all updates never reached the cache.
	 */
	[TestMethod]
	public async Task TheVersionIsEightBytesAndAlwaysGrows()
	{
		await using var connection = await Open();

		await Execute(connection, PostgreSqlVersion.CreateSequence);

		await using var command = new NpgsqlCommand($"SELECT {PostgreSqlVersion.Next} FROM generate_series(1, 200)", connection);
		await using var reader = await command.ExecuteReaderAsync();

		EntityVersion? previous = null;
		var count = 0;

		while (await reader.ReadAsync())
		{
			var bytes = reader.GetFieldValue<byte[]>(0);

			Assert.AreEqual(8, bytes.Length);

			var version = (EntityVersion?)bytes;

			if (previous is not null)
				Assert.IsTrue(version!.CompareTo(previous) > 0, $"Version {Convert.ToHexString(bytes)} does not follow {previous}.");

			previous = version;
			count++;
		}

		Assert.AreEqual(200, count);
	}

	[TestMethod]
	public async Task AnUpdatedRowGetsAHigherVersionThanTheOneItHad()
	{
		var table = $"t{Guid.NewGuid():N}";

		await using var connection = await Open();

		try
		{
			await Execute(connection, PostgreSqlVersion.CreateSequence);
			await Execute(connection, $"""CREATE TABLE "{table}" (name varchar(8), {Ddl.Column(new Column { Name = "eTag", DataType = DbType.Binary, IsVersion = true })})""");
			await Execute(connection, $"""INSERT INTO "{table}" (name) VALUES ('a')""");

			for (var i = 0; i < 50; i++)
			{
				var before = await Version(connection, table);

				await Execute(connection, $"""UPDATE "{table}" SET name = 'b', "eTag" = {PostgreSqlVersion.Next}""");

				Assert.IsTrue((await Version(connection, table))!.CompareTo(before) > 0, "An update has to move the version forward.");
			}
		}
		finally
		{
			await Execute(connection, $"DROP TABLE IF EXISTS \"{table}\"");
		}
	}

	private static async Task<EntityVersion?> Version(NpgsqlConnection connection, string table)
	{
		await using var command = new NpgsqlCommand($"""SELECT "eTag" FROM "{table}" """, connection);

		return (EntityVersion?)(byte[])(await command.ExecuteScalarAsync())!;
	}

	[TestMethod]
	public async Task AnExistingTableReadsBackWithItsIdentityAndConstraints()
	{
		var table = $"t{Guid.NewGuid():N}";

		await using var connection = await Open();

		try
		{
			await Execute(connection, PostgreSqlVersion.CreateSequence);
			await Execute(connection, $"""
				CREATE TABLE "{table}" (
					{Ddl.Column(new Column { Name = "id", DataType = DbType.Int32, IsIdentity = true })} PRIMARY KEY,
					{Ddl.Column(new Column { Name = "eTag", DataType = DbType.Binary, IsVersion = true })}
				)
				""");

			await using (var columns = new NpgsqlCommand(Columns.ColumnsCommandText("public", table), connection))
			await using (var reader = await columns.ExecuteReaderAsync())
			{
				Assert.IsTrue(await reader.ReadAsync());
				Assert.AreEqual("id", reader.GetString(0));
				Assert.IsTrue(reader.GetBoolean(5), "A GENERATED AS IDENTITY column has to read as identity, or every start alters it.");
			}

			await using var constraints = new NpgsqlCommand(Columns.ConstraintsCommandText("public", table), connection);
			await using var rows = await constraints.ExecuteReaderAsync();

			Assert.IsTrue(await rows.ReadAsync());
			Assert.AreEqual("p", rows.GetString(1));
		}
		finally
		{
			await Execute(connection, $"DROP TABLE IF EXISTS \"{table}\"");
		}
	}

	[TestMethod]
	[DataRow(DbType.Int32, 0, "0", "0")]
	[DataRow(DbType.Int32, 0, "7", "7")]
	[DataRow(DbType.String, 16, "it's", "it's")]
	[DataRow(DbType.Boolean, 0, "True", "true")]
	[DataRow(DbType.Decimal, 0, "1.5", "1.5")]
	[DataRow(DbType.DateTime2, 0, "2020-01-02 03:04:05", "2020-01-02 03:04:05")]
	public async Task AColumnAddedWithADefaultFillsTheExistingRows(DbType type, int length, string defaultValue, string expected)
	{
		var table = $"t{Guid.NewGuid():N}";

		await using var connection = await Open();

		try
		{
			await Execute(connection, $"""CREATE TABLE "{table}" (name varchar(8))""");
			await Execute(connection, $"""INSERT INTO "{table}" (name) VALUES ('a'), ('b')""");
			await Execute(connection, $"""ALTER TABLE "{table}" ADD COLUMN {Ddl.Column(new Column { Name = "added", DataType = type, MaxLength = length, DefaultValue = defaultValue })}""");

			await using var command = new NpgsqlCommand($"""SELECT "added"::text FROM "{table}" """, connection);
			await using var reader = await command.ExecuteReaderAsync();
			var count = 0;

			while (await reader.ReadAsync())
			{
				count++;
				Assert.AreEqual(expected, reader.GetString(0), "Existing rows have to read the default.");
			}

			Assert.AreEqual(2, count);
		}
		finally
		{
			await Execute(connection, $"DROP TABLE IF EXISTS \"{table}\"");
		}
	}

	[TestMethod]
	public async Task AnInsertWithoutAVersionGetsOne()
	{
		var table = $"t{Guid.NewGuid():N}";

		await using var connection = await Open();

		try
		{
			await Execute(connection, PostgreSqlVersion.CreateSequence);
			await Execute(connection, $"""CREATE TABLE "{table}" (name varchar(8), {Ddl.Column(new Column { Name = "eTag", DataType = DbType.Binary, IsVersion = true })})""");
			await Execute(connection, $"""INSERT INTO "{table}" (name) VALUES ('a')""");

			await using var command = new NpgsqlCommand($"""SELECT "eTag" FROM "{table}" """, connection);

			Assert.AreEqual(8, ((byte[])(await command.ExecuteScalarAsync())!).Length);
		}
		finally
		{
			await Execute(connection, $"DROP TABLE IF EXISTS \"{table}\"");
		}
	}

	/*
	 * Take without Skip came out as SQL Server's TOP (n), which PostgreSQL reads as a call to a function top.
	 */
	[TestMethod]
	public async Task ATakeIsALimitAfterTheOrder()
	{
		var select = new SelectExpression(Alias.New(), [], null, null, [new OrderExpression(OrderType.Descending, Expression.Constant(1))], null, false, null, Expression.Constant(5), false);
		var text = PostgreSqlFormatter.Format(new ExpressionCompilationContext(new PostgreSqlLanguage()), select);

		Assert.IsFalse(text.Contains("TOP"), text);
		StringAssert.EndsWith(text.TrimEnd(), "LIMIT 5");

		await using var connection = await Open();
		await using var command = new NpgsqlCommand(text, connection);
		await using var reader = await command.ExecuteReaderAsync();

		Assert.IsTrue(await reader.ReadAsync());
	}

	/*
	 * ConnectionProvider commits from an async void handler, so a commit that is still running when it returns
	 * races the scope's dispose. SqlClient's commit, rollback and close finish before they return; these have to too.
	 */
	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public async Task ACommitOrRollbackFinishesBeforeItReturns(bool commit)
	{
		var table = $"t{Guid.NewGuid():N}";

		await using var setup = await Open();

		try
		{
			await Execute(setup, $"""CREATE TABLE "{table}" (name varchar(8))""");

			var connection = new PostgreSqlDataConnection(null!);

			await connection.Initialize(new TestConnectionDto { ConnectionString = TestConfiguration.From(Variable) });
			await connection.Execute(new TextCommand(connection, $"""INSERT INTO "{table}" (name) VALUES ('a')"""));

			var end = commit ? connection.Commit() : connection.Rollback();

			Assert.IsTrue(end.IsCompleted, "The transaction has to end before Commit or Rollback returns.");

			await end;
			await connection.DisposeAsync();

			await using var count = new NpgsqlCommand($"""SELECT count(*) FROM "{table}" """, setup);

			Assert.AreEqual(commit ? 1L : 0L, (long)(await count.ExecuteScalarAsync())!);
		}
		finally
		{
			await Execute(setup, $"DROP TABLE IF EXISTS \"{table}\"");
		}
	}

	/// <summary>Runs one command the way the storage does and binds what it handed back to a copy of the entity.</summary>
	private static async Task<(UnnamedRow Entity, int Affected, PostgreSqlStorageOperation Operation)> Run(PostgreSqlDataConnection connection, UnnamedRow entity)
	{
		var operation = await AggregatedCommandBuilder<UnnamedRow>.Build(entity, null, CancellationToken.None);

		Assert.IsNotNull(operation);

		var affected = await connection.Execute(new OperationCommand(connection, operation));
		var bound = entity with { };

		ReturnValueBinder.Bind(operation, bound);

		return (bound, affected, operation);
	}

	private sealed class OperationCommand(IStorageConnection connection, PostgreSqlStorageOperation operation) : IStorageCommand
	{
		public IStorageOperation Operation { get; } = operation;
		public IStorageConnection Connection { get; } = connection;

		public void Dispose()
		{
		}

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	/// <summary>
	/// The version on a real server: an insert and an update hand back the version they wrote, so that the entity they return can be written
	/// again; a stale version is refused; an entity without one is updated as before.
	/// </summary>
	[TestMethod]
	public async Task TheVersionIsNamedHandedBackAndRefusedWhenStale()
	{
		await using var setup = await Open();

		try
		{
			await Execute(setup, "CREATE SCHEMA IF NOT EXISTS \"test\"");
			await Execute(setup, PostgreSqlVersion.CreateSequence);
			await Execute(setup, "DROP TABLE IF EXISTS \"test\".\"unnamedRow\"");
			await Execute(setup, $"""CREATE TABLE "test"."unnamedRow" ("id" int GENERATED ALWAYS AS IDENTITY PRIMARY KEY, "first" varchar(32), "second" int, {Ddl.Column(new Column { Name = "eTag", DataType = DbType.Binary, IsVersion = true })})""");

			// Disposed before the table is dropped, whatever happens in between: an open transaction would hold the table's locks.
			await using var connection = new PostgreSqlDataConnection(null!);

			await connection.Initialize(new TestConnectionDto { ConnectionString = TestConfiguration.From(Variable) });

			var inserted = await Run(connection, new UnnamedRow { State = State.Add, First = "a", Second = 1 });

			Assert.AreEqual(1, inserted.Affected);
			Assert.AreNotEqual(0, inserted.Entity.Id);
			Assert.IsFalse(string.IsNullOrEmpty(inserted.Entity.ETag), "the insert returned no version");

			var first = await Run(connection, inserted.Entity with { State = State.Update, First = "b" });

			Assert.AreEqual(1, first.Affected, "the update of the entity the insert returned was refused");
			Assert.AreEqual(DataConcurrencyMode.Enabled, first.Operation.Concurrency);
			Assert.AreNotEqual(inserted.Entity.ETag, first.Entity.ETag, "the update returned the version it replaced");

			var second = await Run(connection, first.Entity with { State = State.Update, First = "c" });

			Assert.AreEqual(1, second.Affected, "the update of the entity the update returned was refused");
			Assert.AreNotEqual(first.Entity.ETag, second.Entity.ETag);

			var stale = await Run(connection, inserted.Entity with { State = State.Update, First = "stale" });

			Assert.AreEqual(0, stale.Affected);

			var unversioned = await Run(connection, new UnnamedRow { State = State.Update, Id = inserted.Entity.Id, First = "d", Second = 1 });

			Assert.AreEqual(1, unversioned.Affected);
			Assert.AreEqual(DataConcurrencyMode.Disabled, unversioned.Operation.Concurrency);
			Assert.IsFalse(string.IsNullOrEmpty(unversioned.Entity.ETag), "the update of an entity without a version returned none");

			var again = await Run(connection, unversioned.Entity with { State = State.Update, First = "e" });

			Assert.AreEqual(1, again.Affected);

			await connection.Commit();
		}
		finally
		{
			await Execute(setup, "DROP TABLE IF EXISTS \"test\".\"unnamedRow\"");
		}
	}

	/*
	 * ALTER COLUMN of an existing column: one statement, so that the server applies it as a whole. The version keeps the default
	 * that generates it, a type change has no USING (without it the server only converts what it can assign), a default is a
	 * literal as when the column is created, and an identity column is left without DROP or SET DEFAULT (the server refuses both).
	 */
	private static string AlterStatement(IReadOnlyList<string> commands)
	{
		var statements = commands.Where(c => c.StartsWith("ALTER TABLE", StringComparison.Ordinal)).ToList();

		Assert.AreEqual(1, statements.Count, "The column has to be altered by one statement: " + string.Join("; ", commands));

		return statements[0];
	}

	[TestMethod]
	public void AnAlterIsOneStatement()
	{
		var commands = Ddl.Alter(new Column { Name = "amount", DataType = DbType.Int64, DefaultValue = "0" });

		Assert.AreEqual(1, commands.Count, string.Join("; ", commands));
		Assert.AreEqual(
			"ALTER TABLE \"t\" ALTER COLUMN \"amount\" DROP DEFAULT, ALTER COLUMN \"amount\" TYPE BIGINT, ALTER COLUMN \"amount\" SET NOT NULL, ALTER COLUMN \"amount\" SET DEFAULT '0'",
			commands[0]);
	}

	[TestMethod]
	public void AlteringAVersionColumnKeepsItsDefault()
	{
		var commands = Ddl.Alter(new Column { Name = "eTag", DataType = DbType.Binary, MaxLength = 8, IsVersion = true });

		Assert.AreEqual(2, commands.Count, string.Join("; ", commands));
		Assert.AreEqual(PostgreSqlVersion.CreateSequence, commands[0]);
		StringAssert.EndsWith(AlterStatement(commands), $"ALTER COLUMN \"eTag\" SET DEFAULT {PostgreSqlVersion.Next}");
	}

	[TestMethod]
	public void ATypeChangeHasNoCastThatCutsValues()
	{
		var commands = Ddl.Alter(new Column { Name = "name", DataType = DbType.String, MaxLength = 20 });

		StringAssert.Contains(AlterStatement(commands), "ALTER COLUMN \"name\" TYPE VARCHAR(20)");
		Assert.IsFalse(commands.Any(c => c.Contains("USING")), string.Join("; ", commands));
	}

	[TestMethod]
	public void TheOldDefaultIsDroppedAndTheNewOneSet()
	{
		var statement = AlterStatement(Ddl.Alter(new Column { Name = "amount", DataType = DbType.Int64, DefaultValue = "0" }));

		// The server runs the subcommands in its own order (drop default, type, set default), which the live tests below prove.
		StringAssert.Contains(statement, "ALTER COLUMN \"amount\" DROP DEFAULT");
		StringAssert.Contains(statement, "ALTER COLUMN \"amount\" SET DEFAULT '0'");
	}

	[TestMethod]
	public void AnAlteredDefaultIsALiteralAndNoDefaultDropsIt()
	{
		var withDefault = AlterStatement(Ddl.Alter(new Column { Name = "name", DataType = DbType.String, MaxLength = 10, DefaultValue = "it's" }));
		var without = AlterStatement(Ddl.Alter(new Column { Name = "name", DataType = DbType.String, MaxLength = 10 }));

		StringAssert.Contains(withDefault, "ALTER COLUMN \"name\" SET DEFAULT 'it''s'");
		StringAssert.Contains(without, "ALTER COLUMN \"name\" DROP DEFAULT");
		Assert.IsFalse(without.Contains("SET DEFAULT"), without);
	}

	[TestMethod]
	public void AnIdentityColumnIsNotGivenOrStrippedOfADefault()
	{
		var commands = Ddl.Alter(new Column { Name = "id", DataType = DbType.Int32, IsIdentity = true });

		Assert.IsFalse(commands.Any(c => c.Contains("DEFAULT")), string.Join("; ", commands));
		Assert.AreEqual("ALTER TABLE \"t\" ALTER COLUMN \"id\" TYPE INTEGER, ALTER COLUMN \"id\" SET NOT NULL", AlterStatement(commands));
	}

	[TestMethod]
	public void AnIdentityColumnWithADefaultValueIsNotGivenOne()
	{
		var commands = Ddl.Alter(new Column { Name = "id", DataType = DbType.Int32, IsIdentity = true, DefaultValue = "1" });

		Assert.IsFalse(commands.Any(c => c.Contains("DEFAULT")), string.Join("; ", commands));
	}

	/// <summary>A database of its own on the server the variable names, dropped when the test is done.</summary>
	/// <remarks>
	/// The tests that alter columns create the shared version sequence in it, so that nothing they do reaches the sequence, or the
	/// defaults that use it, in a database somebody else works in.
	/// </remarks>
	private sealed class ScratchDatabase : IAsyncDisposable
	{
		private readonly string _admin;
		private readonly string _name;

		private ScratchDatabase(string admin, string name, NpgsqlConnection connection)
		{
			_admin = admin;
			_name = name;
			Connection = connection;
		}

		public NpgsqlConnection Connection { get; }

		public static async Task<ScratchDatabase> Create()
		{
			var admin = TestConfiguration.From(Variable);
			var name = $"connected_{Guid.NewGuid():N}";

			await using (var server = new NpgsqlConnection(admin))
			{
				await server.OpenAsync();
				await Execute(server, $"CREATE DATABASE \"{name}\"");
			}

			var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(admin) { Database = name, Pooling = false }.ConnectionString);

			await connection.OpenAsync();

			return new ScratchDatabase(admin, name, connection);
		}

		public async ValueTask DisposeAsync()
		{
			await Connection.DisposeAsync();

			await using var server = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(_admin) { Pooling = false }.ConnectionString);

			await server.OpenAsync();
			await Execute(server, $"DROP DATABASE IF EXISTS \"{_name}\" WITH (FORCE)");
		}
	}

	private sealed record Described(string DataType, bool IsNullable, string? Default, int? Length, bool IsIdentity);

	private static async Task<Described> Describe(NpgsqlConnection connection, string table, string column)
	{
		await using var command = new NpgsqlCommand(
			"SELECT data_type, is_nullable, column_default, character_maximum_length, is_identity FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @table AND column_name = @column",
			connection);

		command.Parameters.AddWithValue("table", table);
		command.Parameters.AddWithValue("column", column);

		await using var reader = await command.ExecuteReaderAsync();

		Assert.IsTrue(await reader.ReadAsync(), $"{table}.{column} does not exist.");

		return new Described(reader.GetString(0), reader.GetString(1) == "YES", reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetInt32(3), reader.GetString(4) == "YES");
	}

	private static async Task Alter(NpgsqlConnection connection, string table, ISchemaColumn column)
	{
		foreach (var command in Ddl.AlterOn($"\"public\".\"{table}\"", column))
			await Execute(connection, command);
	}

	private static async Task<object?> Scalar(NpgsqlConnection connection, string text)
	{
		await using var command = new NpgsqlCommand(text, connection);

		return await command.ExecuteScalarAsync();
	}

	[TestMethod]
	public async Task AnAlteredVersionColumnKeepsItsDefaultAndGivesNewRowsAnEightByteVersion()
	{
		var table = $"t{Guid.NewGuid():N}";

		await using var database = await ScratchDatabase.Create();
		var connection = database.Connection;
		var version = new Column { Name = "eTag", DataType = DbType.Binary, MaxLength = 8, IsVersion = true };

		await Execute(connection, $"""CREATE TABLE "{table}" ("id" INTEGER NOT NULL, "eTag" BYTEA NOT NULL)""");
		await Execute(connection, $"""INSERT INTO "{table}" VALUES (1, '\x01'), (2, '\x02')""");

		// What Express does on every start: the second alter finds the column as the first one left it.
		for (var i = 0; i < 2; i++)
		{
			await Alter(connection, table, version);

			var described = await Describe(connection, table, "eTag");

			Assert.AreEqual("bytea", described.DataType);
			Assert.IsFalse(described.IsNullable);
			Assert.IsNotNull(described.Default, "the version column lost its default");
			StringAssert.Contains(described.Default, "int8send");
			StringAssert.Contains(described.Default, "connectedVersion");
		}

		await Execute(connection, $"""INSERT INTO "{table}" ("id") VALUES (3)""");

		Assert.AreEqual(8, Convert.ToInt32(await Scalar(connection, $"""SELECT length("eTag") FROM "{table}" WHERE "id" = 3""")));
		Assert.AreEqual(3L, (long)(await Scalar(connection, $"""SELECT count(*) FROM "{table}" """))!);
		Assert.AreEqual(1, Convert.ToInt32(await Scalar(connection, $"""SELECT length("eTag") FROM "{table}" WHERE "id" = 1""")), "an existing row kept its value");
	}

	[TestMethod]
	[DataRow("INTEGER", DbType.Int64, 0, "7", "'7'::bigint", "7")]
	[DataRow("VARCHAR(5)", DbType.String, 20, "it's", "'it''s'::character varying", "it's")]
	[DataRow("VARCHAR(5)", DbType.String, 20, "a-b c", "'a-b c'::character varying", "a-b c")]
	public async Task AnAlteredDefaultIsAQuotedLiteralTheNewRowsGet(string oldType, DbType type, int length, string value, string expectedDefault, string expectedValue)
	{
		var table = $"t{Guid.NewGuid():N}";

		await using var database = await ScratchDatabase.Create();
		var connection = database.Connection;
		var column = new Column { Name = "amount", DataType = type, MaxLength = length, DefaultValue = value };

		await Execute(connection, $"""CREATE TABLE "{table}" ("id" INTEGER NOT NULL, "amount" {oldType} NOT NULL DEFAULT 5)""");
		await Execute(connection, $"""INSERT INTO "{table}" ("id", "amount") VALUES (1, '5')""");

		for (var i = 0; i < 2; i++)
		{
			await Alter(connection, table, column);

			Assert.AreEqual(expectedDefault, (await Describe(connection, table, "amount")).Default);
		}

		await Execute(connection, $"""INSERT INTO "{table}" ("id") VALUES (2)""");

		Assert.AreEqual("5", (await Scalar(connection, $"""SELECT "amount"::text FROM "{table}" WHERE "id" = 1"""))!.ToString());
		Assert.AreEqual(expectedValue, (await Scalar(connection, $"""SELECT "amount"::text FROM "{table}" WHERE "id" = 2"""))!.ToString());
	}

	[TestMethod]
	public async Task AnAlterSetsAndDropsNotNull()
	{
		var table = $"t{Guid.NewGuid():N}";

		await using var database = await ScratchDatabase.Create();
		var connection = database.Connection;

		await Execute(connection, $"""CREATE TABLE "{table}" ("id" INTEGER NOT NULL, "a" INTEGER NULL, "b" INTEGER NOT NULL)""");
		await Execute(connection, $"""INSERT INTO "{table}" VALUES (1, 1, 1)""");

		await Alter(connection, table, new Column { Name = "a", DataType = DbType.Int32, IsNullable = false });
		await Alter(connection, table, new Column { Name = "b", DataType = DbType.Int32, IsNullable = true });

		Assert.IsFalse((await Describe(connection, table, "a")).IsNullable, "SET NOT NULL");
		Assert.IsTrue((await Describe(connection, table, "b")).IsNullable, "DROP NOT NULL");
	}

	/*
	 * The old default and the new one in one statement: the new one replaces it whatever the old one was (100000 does not fit the
	 * smallint it is cast to, which the server only finds out when it is used), and a column without a default drops it.
	 */
	[TestMethod]
	public async Task TheNewDefaultReplacesTheOldOneAndNoDefaultDropsIt()
	{
		var table = $"t{Guid.NewGuid():N}";

		await using var database = await ScratchDatabase.Create();
		var connection = database.Connection;

		await Execute(connection, $"""CREATE TABLE "{table}" ("count" INTEGER NOT NULL DEFAULT 100000)""");

		await Alter(connection, table, new Column { Name = "count", DataType = DbType.Int16, DefaultValue = "5" });

		var described = await Describe(connection, table, "count");

		Assert.AreEqual("smallint", described.DataType);
		Assert.AreEqual("'5'::smallint", described.Default);

		await Execute(connection, $"""INSERT INTO "{table}" DEFAULT VALUES""");

		Assert.AreEqual(5, Convert.ToInt32(await Scalar(connection, $"""SELECT "count" FROM "{table}" """)));

		await Alter(connection, table, new Column { Name = "count", DataType = DbType.Int16 });

		Assert.IsNull((await Describe(connection, table, "count")).Default, "no default has to drop the old one");
	}

	/*
	 * A conversion the server has no assignment cast for (text to integer) is refused even on an empty table, as the type change has
	 * no USING, and the column, its text default included, stays as it was.
	 */
	[TestMethod]
	public async Task AConversionTheServerCannotDoIsRefusedAndLeavesTheColumnAsItWas()
	{
		var table = $"t{Guid.NewGuid():N}";

		await using var database = await ScratchDatabase.Create();
		var connection = database.Connection;

		await Execute(connection, $"""CREATE TABLE "{table}" ("count" TEXT NOT NULL DEFAULT 'abc')""");

		var before = await Describe(connection, table, "count");
		var thrown = await Assert.ThrowsExceptionAsync<PostgresException>(() => Alter(connection, table, new Column { Name = "count", DataType = DbType.Int32, DefaultValue = "5" }));

		Assert.AreEqual(PostgresErrorCodes.DatatypeMismatch, thrown.SqlState);
		Assert.AreEqual(before, await Describe(connection, table, "count"));
		Assert.AreEqual("'abc'::text", before.Default);
	}

	[TestMethod]
	public async Task AnAlteredIdentityColumnStaysAnIdentityAndKeepsGeneratingValues()
	{
		var table = $"t{Guid.NewGuid():N}";

		await using var database = await ScratchDatabase.Create();
		var connection = database.Connection;

		await Execute(connection, $"""CREATE TABLE "{table}" ("id" INTEGER GENERATED BY DEFAULT AS IDENTITY NOT NULL, "name" TEXT NULL)""");
		await Execute(connection, $"""INSERT INTO "{table}" ("name") VALUES ('a'), ('b')""");

		for (var i = 0; i < 2; i++)
		{
			await Alter(connection, table, new Column { Name = "id", DataType = DbType.Int64, IsIdentity = true });

			var described = await Describe(connection, table, "id");

			Assert.AreEqual("bigint", described.DataType);
			Assert.IsTrue(described.IsIdentity);
			Assert.IsNull(described.Default);
		}

		await Execute(connection, $"""INSERT INTO "{table}" ("name") VALUES ('c')""");

		Assert.AreEqual(3L, Convert.ToInt64(await Scalar(connection, $"""SELECT max("id") FROM "{table}" """)));
		Assert.AreEqual(3L, Convert.ToInt64(await Scalar(connection, $"""SELECT count(*) FROM "{table}" """)));
	}

	/*
	 * An alter of a table that holds rows: a widened type keeps them, and a string that would be cut is refused with "value too long"
	 * (22001) by the one statement, which leaves the column exactly as it was: type, nullability and default. Run as separate
	 * statements, the DROP DEFAULT before it would stay committed.
	 */
	[TestMethod]
	public async Task AnAlterKeepsTheRowsAndRefusesToCutThemLeavingTheColumnAsItWas()
	{
		var table = $"t{Guid.NewGuid():N}";

		await using var database = await ScratchDatabase.Create();
		var connection = database.Connection;

		await Execute(connection, $"""CREATE TABLE "{table}" ("id" INTEGER NOT NULL, "amount" INTEGER NOT NULL DEFAULT 0, "name" VARCHAR(100) NOT NULL DEFAULT 'abc')""");
		await Execute(connection, $"""INSERT INTO "{table}" VALUES (1, 5, '{new string('x', 50)}')""");

		await Alter(connection, table, new Column { Name = "amount", DataType = DbType.Int64, DefaultValue = "7" });
		await Execute(connection, $"""INSERT INTO "{table}" ("id", "name") VALUES (2, 'y')""");

		Assert.AreEqual(5L, Convert.ToInt64(await Scalar(connection, $"""SELECT "amount" FROM "{table}" WHERE "id" = 1""")));
		Assert.AreEqual(7L, Convert.ToInt64(await Scalar(connection, $"""SELECT "amount" FROM "{table}" WHERE "id" = 2""")));

		var before = await Describe(connection, table, "name");

		Assert.AreEqual("'abc'::character varying", before.Default);

		var shrink = Ddl.AlterOn($"\"public\".\"{table}\"", new Column { Name = "name", DataType = DbType.String, MaxLength = 20, IsNullable = true, DefaultValue = "zz" });

		var thrown = await Assert.ThrowsExceptionAsync<PostgresException>(async () =>
		{
			foreach (var command in shrink)
				await Execute(connection, command);
		});

		Assert.AreEqual(PostgresErrorCodes.StringDataRightTruncation, thrown.SqlState);
		Assert.AreEqual(before, await Describe(connection, table, "name"), "a refused alter has to leave the column, its default included, as it was");
		Assert.AreEqual(50, Convert.ToInt32(await Scalar(connection, $"""SELECT max(length("name")) FROM "{table}" """)));
	}

	/*
	 * A bytea type is written as bytea, whatever length the type carries: the default size of a binary type is int.MaxValue,
	 * which the formatter wrote as TEXT (the type of a DECLARE of a byte[] variable), and a declared length as bytea(n), which the
	 * server refuses.
	 */
	[TestMethod]
	[DataRow(int.MaxValue)]
	[DataRow(8)]
	public void AByteaTypeIsFormattedWithoutALength(int length)
	{
		var text = new PostgreSqlTypeSystem().Format(new PostgreSqlDataType(NpgsqlDbType.Bytea, true, length, 0, 0), false);

		Assert.AreEqual("bytea", text);
	}

	[TestMethod]
	[DataRow(NpgsqlDbType.Varchar, 50, false, "varchar(50)")]
	[DataRow(NpgsqlDbType.Varchar, 50, true, "varchar")]
	[DataRow(NpgsqlDbType.Varchar, int.MaxValue, false, "TEXT")]
	[DataRow(NpgsqlDbType.Text, int.MaxValue, false, "text")]
	[DataRow(NpgsqlDbType.Bytea, 8, true, "bytea")]
	public void TheOtherTypesAreFormattedAsBefore(NpgsqlDbType type, int length, bool suppressSize, string expected)
	{
		Assert.AreEqual(expected, new PostgreSqlTypeSystem().Format(new PostgreSqlDataType(type, true, length, 0, 0), suppressSize), true);
	}

	private static async Task<NpgsqlConnection> Open()
	{
		var connection = new NpgsqlConnection(TestConfiguration.From(Variable));

		await connection.OpenAsync();

		return connection;
	}

	private static async Task Execute(NpgsqlConnection connection, string text)
	{
		await using var command = new NpgsqlCommand(text, connection);

		await command.ExecuteNonQueryAsync();
	}

	private abstract class Ddl : SynchronizationCommand
	{
		public static string Column(ISchemaColumn column) => CreateColumnCommandText(column);
		public static IReadOnlyList<string> Alter(ISchemaColumn column) => AlterColumnCommands("\"t\"", column);
		public static IReadOnlyList<string> AlterOn(string table, ISchemaColumn column) => AlterColumnCommands(table, column);
	}

	private sealed class Column : ISchemaColumn
	{
		public required string Name { get; init; }
		public DbType DataType { get; init; }
		public bool IsIdentity { get; init; }
		public bool IsUnique { get; init; }
		public bool IsIndex { get; init; }
		public bool IsPrimaryKey { get; init; }
		public bool IsVersion { get; init; }
		public string? DefaultValue { get; init; }
		public int MaxLength { get; init; }
		public bool IsNullable { get; init; }
		public string? Index { get; init; }
		public int Scale { get; init; }
		public int Precision { get; init; }
		public DateKind DateKind { get; init; }
		public BinaryKind BinaryKind { get; init; }
		public int DatePrecision { get; init; }
		public PropertyInfo? Property { get; init; }
	}
}
