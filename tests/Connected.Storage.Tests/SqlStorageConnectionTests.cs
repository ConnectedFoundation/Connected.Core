using Connected.Storage.Sql;
using Microsoft.Data.SqlClient;

namespace Connected.Storage.Tests;

/// <summary>
/// Pins how the SQL Server connection ends a transaction, against the server in <c>CONNECTED_TEST_SQLSERVER</c>.
/// </summary>
/// <remarks>
/// The PostgreSQL provider is made to match this; see <c>PostgreSqlProviderTests.ACommitOrRollbackFinishesBeforeItReturns</c>.
/// </remarks>
[TestClass]
public sealed class SqlStorageConnectionTests
{
	private const string Variable = "CONNECTED_TEST_SQLSERVER";

	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public async Task ACommitOrRollbackFinishesBeforeItReturns(bool commit)
	{
		var table = $"##t{Guid.NewGuid():N}";

		await using var setup = new SqlConnection(TestConfiguration.From(Variable));

		await setup.OpenAsync();
		await using (var create = new SqlCommand($"CREATE TABLE [{table}] (name varchar(8))", setup))
			await create.ExecuteNonQueryAsync();

		var connection = new SqlDataConnection(null!);

		await connection.Initialize(new TestConnectionDto { ConnectionString = TestConfiguration.From(Variable) });
		await connection.Execute(new TextCommand(connection, $"INSERT INTO [{table}] (name) VALUES ('a')"));

		var end = commit ? connection.Commit() : connection.Rollback();

		Assert.IsTrue(end.IsCompleted, "The transaction has to end before Commit or Rollback returns.");

		await end;
		await connection.DisposeAsync();

		await using var count = new SqlCommand($"SELECT count(*) FROM [{table}]", setup);

		Assert.AreEqual(commit ? 1 : 0, (int)(await count.ExecuteScalarAsync())!);
	}
}
