using Connected.Storage.PostgreSql.Containers;
using Npgsql;

namespace Connected.Storage.Tests;

/// <summary>
/// Runs the PostgreSQL container provider against the server in <c>CONNECTED_TEST_POSTGRES</c>.
/// </summary>
[TestClass]
public sealed class PostgreSqlStorageContainerProviderTests
{
	private const string Variable = "CONNECTED_TEST_POSTGRES";

	[TestMethod]
	public async Task CreatesAContainerBesideTheDefaultDatabaseAndDropsIt()
	{
		var provider = new PostgreSqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";
		var connectionString = await provider.Create(TestConfiguration.Container(name));

		try
		{
			Assert.AreEqual(name, new NpgsqlConnectionStringBuilder(connectionString).Database);
			Assert.IsTrue(await Exists(name));

			/*
			 * Creating an existing container changes nothing.
			 */
			Assert.AreEqual(connectionString, await provider.Create(TestConfiguration.Container(name)));
		}
		finally
		{
			await provider.Drop(TestConfiguration.Container(name));
		}

		Assert.IsFalse(await Exists(name));
	}

	[TestMethod]
	public async Task DropsAContainerWithOpenConnections()
	{
		var provider = new PostgreSqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";
		var connectionString = await provider.Create(TestConfiguration.Container(name));

		await using (var open = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString))
		{
			await open.OpenAsync();
			await provider.Drop(TestConfiguration.Container(name));
		}

		Assert.IsFalse(await Exists(name));
	}

	[TestMethod]
	public async Task DroppingAMissingContainerDoesNothing()
	{
		var provider = new PostgreSqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));

		await provider.Drop(TestConfiguration.Container($"connected_test_{Guid.NewGuid():N}"));
	}

	[TestMethod]
	public async Task TheDefaultDatabaseIsNeverDropped()
	{
		var defaultConnection = TestConfiguration.From(Variable);
		var provider = new PostgreSqlStorageContainerProvider(new TestConfiguration(defaultConnection));
		var name = new NpgsqlConnectionStringBuilder(defaultConnection).Database!;

		await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.Drop(TestConfiguration.Container(name)));
		Assert.IsTrue(await Exists(name));
	}

	private static async Task<bool> Exists(string name)
	{
		await using var connection = new NpgsqlConnection(TestConfiguration.From(Variable));

		await connection.OpenAsync();

		await using var command = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);

		command.Parameters.AddWithValue("name", name);

		return await command.ExecuteScalarAsync() is not null;
	}
}
