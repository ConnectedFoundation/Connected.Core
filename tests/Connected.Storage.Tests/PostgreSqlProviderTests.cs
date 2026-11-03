using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;
using Connected.Storage.PostgreSql;
using Connected.Storage.PostgreSql.Schemas;
using Connected.Storage.PostgreSql.Transactions;
using Connected.Storage.Schemas;
using Npgsql;
using System.Data;
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
		Assert.IsFalse((await Build(State.Add)).CommandText.Contains("eTag"));
	}

	[TestMethod]
	public void AVersionColumnGetsADefault()
	{
		StringAssert.Contains(Ddl.Column(new Column { Name = "eTag", DataType = DbType.Binary, IsVersion = true }), $"DEFAULT {PostgreSqlVersion.Next}");
	}

	[TestMethod]
	public async Task TheVersionIsEightBytesAndChanges()
	{
		await using var connection = await Open();
		await using var command = new NpgsqlCommand($"SELECT {PostgreSqlVersion.Next}, {PostgreSqlVersion.Next}", connection);
		await using var reader = await command.ExecuteReaderAsync();

		Assert.IsTrue(await reader.ReadAsync());

		var first = reader.GetFieldValue<byte[]>(0);

		Assert.AreEqual(8, first.Length);
		CollectionAssert.AreNotEqual(first, reader.GetFieldValue<byte[]>(1));
	}

	[TestMethod]
	public async Task AnExistingTableReadsBackWithItsIdentityAndConstraints()
	{
		var table = $"t{Guid.NewGuid():N}";

		await using var connection = await Open();

		try
		{
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
	public async Task AnInsertWithoutAVersionGetsOne()
	{
		var table = $"t{Guid.NewGuid():N}";

		await using var connection = await Open();

		try
		{
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
