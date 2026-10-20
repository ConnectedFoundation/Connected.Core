using Connected.Storage.Sql.Containers;
using Microsoft.Data.SqlClient;

namespace Connected.Storage.Tests;

/// <summary>
/// Runs the SQL Server container provider against the server in <c>CONNECTED_TEST_SQLSERVER</c>.
/// </summary>
[TestClass]
public sealed class SqlStorageContainerProviderTests
{
	private const string Variable = "CONNECTED_TEST_SQLSERVER";

	[TestMethod]
	public async Task CreatesAContainerBesideTheDefaultDatabaseAndDropsIt()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";
		var connectionString = await provider.Create(TestConfiguration.Container(name));

		try
		{
			Assert.AreEqual(name, new SqlConnectionStringBuilder(connectionString).InitialCatalog);
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
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";
		var connectionString = await provider.Create(TestConfiguration.Container(name));

		await using (var open = new SqlConnection(new SqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString))
		{
			await open.OpenAsync();
			await provider.Drop(TestConfiguration.Container(name));
		}

		Assert.IsFalse(await Exists(name));
	}

	[TestMethod]
	public async Task DroppingAMissingContainerDoesNothing()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));

		await provider.Drop(TestConfiguration.Container($"connected_test_{Guid.NewGuid():N}"));
	}

	[TestMethod]
	public async Task TheDefaultDatabaseIsNeverDropped()
	{
		var defaultConnection = TestConfiguration.From(Variable);
		var provider = new SqlStorageContainerProvider(new TestConfiguration(defaultConnection));
		var name = new SqlConnectionStringBuilder(defaultConnection).InitialCatalog;

		await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.Drop(TestConfiguration.Container(name)));
		Assert.IsTrue(await Exists(name));
	}

	[TestMethod]
	public async Task StorageThatIsNotAContainerIsNeitherAdoptedNorDropped()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";

		await Run($"CREATE DATABASE [{name}]");

		try
		{
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.Create(TestConfiguration.Container(name)));
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.Drop(TestConfiguration.Container(name)));
			Assert.IsTrue(await Exists(name));
		}
		finally
		{
			await Run($"DROP DATABASE [{name}]");
		}
	}

	[TestMethod]
	public async Task ConcurrentCreatesYieldOneContainer()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";

		try
		{
			var connectionStrings = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => provider.Create(TestConfiguration.Container(name))));

			Assert.AreEqual(1, connectionStrings.Distinct().Count());
			Assert.IsTrue(await Exists(name));
		}
		finally
		{
			await provider.Drop(TestConfiguration.Container(name));
		}

		Assert.IsFalse(await Exists(name));
	}

	[TestMethod]
	public async Task AContainerLeftOfflineByAnInterruptedDropIsDropped()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";

		await provider.Create(TestConfiguration.Container(name));
		await Run($"ALTER DATABASE [{name}] SET OFFLINE");
		await provider.Drop(TestConfiguration.Container(name));

		Assert.IsFalse(await Exists(name));
	}

	[TestMethod]
	public async Task OfflineStorageThatIsNotAContainerIsLeftOffline()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";

		await Run($"CREATE DATABASE [{name}]");
		await Run($"ALTER DATABASE [{name}] SET OFFLINE");

		try
		{
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.Drop(TestConfiguration.Container(name)));
			Assert.IsTrue(await Exists(name));
			Assert.AreEqual("OFFLINE", await State(name));
		}
		finally
		{
			await Run($"ALTER DATABASE [{name}] SET ONLINE; DROP DATABASE [{name}]");
		}
	}

	private static async Task<string?> State(string name)
	{
		await using var connection = new SqlConnection(TestConfiguration.From(Variable));

		await connection.OpenAsync();

		await using var command = new SqlCommand("SELECT state_desc FROM sys.databases WHERE name = @name", connection);

		command.Parameters.AddWithValue("@name", name);

		return await command.ExecuteScalarAsync() as string;
	}

	private static async Task Run(string text)
	{
		await using var connection = new SqlConnection(TestConfiguration.From(Variable));

		await connection.OpenAsync();

		await using var command = new SqlCommand(text, connection);

		await command.ExecuteNonQueryAsync();
	}

	private static async Task<bool> Exists(string name)
	{
		await using var connection = new SqlConnection(TestConfiguration.From(Variable));

		await connection.OpenAsync();

		await using var command = new SqlCommand("SELECT DB_ID(@name)", connection);

		command.Parameters.AddWithValue("@name", name);

		return await command.ExecuteScalarAsync() is not (null or DBNull);
	}
}
