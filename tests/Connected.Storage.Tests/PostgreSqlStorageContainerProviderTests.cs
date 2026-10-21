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
		/*
		 * As the server resolves it, because the connection string may name no database.
		 */
		var name = await Scalar("SELECT current_database()") as string;

		Assert.IsFalse(string.IsNullOrEmpty(name));
		await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.Drop(TestConfiguration.Container(name)));
		Assert.IsTrue(await Exists(name));
	}

	[TestMethod]
	public async Task StorageThatIsNotAContainerIsNeitherAdoptedNorDropped()
	{
		var provider = new PostgreSqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";

		await Run($"CREATE DATABASE \"{name}\"");

		try
		{
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.Create(TestConfiguration.Container(name)));
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.Drop(TestConfiguration.Container(name)));
			Assert.IsTrue(await Exists(name));
		}
		finally
		{
			await Run($"DROP DATABASE \"{name}\"");
		}
	}

	[TestMethod]
	public async Task ConcurrentCreatesYieldOneContainer()
	{
		var provider = new PostgreSqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
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
	public async Task AMarkedStagingDatabaseLeftByACreateIsRemovedByALaterDrop()
	{
		var provider = new PostgreSqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var staging = Staging(TimeSpan.FromHours(2));

		/*
		 * A create interrupted after the marker, before the rename.
		 */
		await Run($"CREATE DATABASE \"{staging}\"");
		await Run($"COMMENT ON DATABASE \"{staging}\" IS 'connected:storage-container'");
		await provider.Drop(TestConfiguration.Container($"connected_test_{Guid.NewGuid():N}"));

		Assert.IsFalse(await Exists(staging));
	}

	[TestMethod]
	[DataRow(0, true)]
	[DataRow(2, false)]
	public async Task AStagingDatabaseACreateMayStillBeWorkingOnOrThatIsUnmarkedIsNotRemoved(int hoursOld, bool marked)
	{
		var provider = new PostgreSqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var staging = Staging(TimeSpan.FromHours(hoursOld));

		await Run($"CREATE DATABASE \"{staging}\"");

		if (marked)
			await Run($"COMMENT ON DATABASE \"{staging}\" IS 'connected:storage-container'");

		try
		{
			await provider.Drop(TestConfiguration.Container($"connected_test_{Guid.NewGuid():N}"));

			Assert.IsTrue(await Exists(staging));
		}
		finally
		{
			await Run($"DROP DATABASE \"{staging}\"");
		}
	}

	private static string Staging(TimeSpan age) => $"connected_staging_{(DateTimeOffset.UtcNow - age).ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture)}_{Guid.NewGuid().ToString("N")[..16]}";

	private static async Task<object?> Scalar(string text)
	{
		await using var connection = new NpgsqlConnection(TestConfiguration.From(Variable));

		await connection.OpenAsync();

		await using var command = new NpgsqlCommand(text, connection);

		return await command.ExecuteScalarAsync();
	}

	private static async Task Run(string text)
	{
		await using var connection = new NpgsqlConnection(TestConfiguration.From(Variable));

		await connection.OpenAsync();

		await using var command = new NpgsqlCommand(text, connection);

		await command.ExecuteNonQueryAsync();
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
