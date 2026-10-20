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
		/*
		 * As the server resolves it, because the connection string may name no database.
		 */
		var name = await Scalar("SELECT DB_NAME()") as string;

		Assert.IsFalse(string.IsNullOrEmpty(name));
		await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.Drop(TestConfiguration.Container(name)));
		Assert.IsTrue(await Exists(name));
	}

	[TestMethod]
	public async Task ConcurrentDropsOfOneContainerAllSucceed()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";

		await provider.Create(TestConfiguration.Container(name));
		await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => provider.Drop(TestConfiguration.Container(name))));

		Assert.IsFalse(await Exists(name));
	}

	[TestMethod]
	public async Task ConcurrentDropsOfDifferentContainersAllSucceed()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var names = Enumerable.Range(0, 3).Select(_ => $"connected_test_{Guid.NewGuid():N}").ToList();

		foreach (var name in names)
			await provider.Create(TestConfiguration.Container(name));

		await Task.WhenAll(names.Select(f => provider.Drop(TestConfiguration.Container(f))));

		foreach (var name in names)
			Assert.IsFalse(await Exists(name));
	}

	[TestMethod]
	public async Task DropsAContainerAClientKeepsReconnectingTo()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";
		var connectionString = new SqlConnectionStringBuilder(await provider.Create(TestConfiguration.Container(name))) { Pooling = false, ConnectTimeout = 2 }.ConnectionString;
		using var stop = new CancellationTokenSource();

		/*
		 * The application's own login, reconnecting as fast as it can.
		 */
		var client = Task.Run(async () =>
		{
			while (!stop.IsCancellationRequested)
			{
				try
				{
					await using var connection = new SqlConnection(connectionString);

					await connection.OpenAsync();
					await Task.Delay(10);
				}
				catch (SqlException)
				{
				}
			}
		});

		try
		{
			await Task.Delay(200);
			await provider.Drop(TestConfiguration.Container(name));
		}
		finally
		{
			await stop.CancelAsync();
			await client;
		}

		Assert.IsFalse(await Exists(name));
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
	public async Task AnAbandonedDropIsRemovedByALaterDrop()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";
		var dropping = Dropping(TimeSpan.FromHours(2));

		/*
		 * A drop interrupted after the rename, two hours ago.
		 */
		await provider.Create(TestConfiguration.Container(name));
		await Run($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; ALTER DATABASE [{name}] MODIFY NAME = [{dropping}]");

		Assert.IsFalse(await Exists(name));

		await provider.Drop(TestConfiguration.Container($"connected_test_{Guid.NewGuid():N}"));

		Assert.IsFalse(await Exists(dropping));
	}

	[TestMethod]
	public async Task ADropInProgressIsNotRemovedByAnotherDrop()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";
		var dropping = Dropping(TimeSpan.Zero);

		await provider.Create(TestConfiguration.Container(name));
		await Run($"ALTER DATABASE [{name}] MODIFY NAME = [{dropping}]");

		try
		{
			await provider.Drop(TestConfiguration.Container($"connected_test_{Guid.NewGuid():N}"));

			Assert.IsTrue(await Exists(dropping));
		}
		finally
		{
			await Run($"DROP DATABASE [{dropping}]");
		}
	}

	[TestMethod]
	public async Task AnUnmarkedDatabaseUnderTheDroppingPrefixIsNotRemoved()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var dropping = Dropping(TimeSpan.FromHours(2));

		await Run($"CREATE DATABASE [{dropping}]");

		try
		{
			await provider.Drop(TestConfiguration.Container($"connected_test_{Guid.NewGuid():N}"));

			Assert.IsTrue(await Exists(dropping));
		}
		finally
		{
			await Run($"DROP DATABASE [{dropping}]");
		}
	}

	[TestMethod]
	public async Task AContainerLeftRestrictedByAnInterruptedDropIsOpenedAgainOnCreate()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";

		await provider.Create(TestConfiguration.Container(name));

		try
		{
			await Run($"ALTER DATABASE [{name}] SET RESTRICTED_USER");
			await provider.Create(TestConfiguration.Container(name));

			Assert.AreEqual("MULTI_USER", await Access(name));
		}
		finally
		{
			await provider.Drop(TestConfiguration.Container(name));
		}
	}

	[TestMethod]
	public async Task AContainerLeftSingleUserIsLeftWhileAClientHoldsItAndOpenedAgainAfter()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";
		var connectionString = new SqlConnectionStringBuilder(await provider.Create(TestConfiguration.Container(name))) { Pooling = false }.ConnectionString;

		try
		{
			await Run($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");

			/*
			 * Its marker cannot be read while the one connection is taken, so it is not touched.
			 */
			await using (var holder = new SqlConnection(connectionString))
			{
				await holder.OpenAsync();

				var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.Create(TestConfiguration.Container(name)));

				StringAssert.Contains(ex.Message, "single-user");
				await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.Drop(TestConfiguration.Container(name)));
				Assert.AreEqual("SINGLE_USER", await Access(name));
			}

			await provider.Create(TestConfiguration.Container(name));

			Assert.AreEqual("MULTI_USER", await Access(name));
		}
		finally
		{
			await provider.Drop(TestConfiguration.Container(name));
		}

		Assert.IsFalse(await Exists(name));
	}

	[TestMethod]
	public async Task AContainerLeftSingleUserIsDropped()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";

		await provider.Create(TestConfiguration.Container(name));
		await Run($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
		await provider.Drop(TestConfiguration.Container(name));

		Assert.IsFalse(await Exists(name));
	}

	[TestMethod]
	public async Task AMarkedStagingDatabaseLeftByACreateIsRemovedByALaterDrop()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var staging = ProviderName("connected_staging_", TimeSpan.FromHours(2));

		/*
		 * A create interrupted after the marker, before the rename.
		 */
		await Run($"CREATE DATABASE [{staging}]; EXEC [{staging}].sys.sp_addextendedproperty @name = N'connected:storage-container', @value = N'true'");
		await provider.Drop(TestConfiguration.Container($"connected_test_{Guid.NewGuid():N}"));

		Assert.IsFalse(await Exists(staging));
	}

	private static string Dropping(TimeSpan age) => ProviderName("connected_dropping_", age);

	private static string ProviderName(string prefix, TimeSpan age) => $"{prefix}{(DateTimeOffset.UtcNow - age).ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture)}_{Guid.NewGuid():N}";

	[TestMethod]
	public async Task AnOfflineContainerUnderItsOwnNameIsLeftAsItIs()
	{
		var provider = new SqlStorageContainerProvider(new TestConfiguration(TestConfiguration.From(Variable)));
		var name = $"connected_test_{Guid.NewGuid():N}";

		await provider.Create(TestConfiguration.Container(name));
		await Run($"ALTER DATABASE [{name}] SET OFFLINE");

		try
		{
			var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.Drop(TestConfiguration.Container(name)));

			StringAssert.Contains(ex.Message, "OFFLINE");
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.Create(TestConfiguration.Container(name)));
			Assert.AreEqual("OFFLINE", await State(name));
		}
		finally
		{
			await Run($"ALTER DATABASE [{name}] SET ONLINE; DROP DATABASE [{name}]");
		}
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

	private static async Task<string?> Access(string name)
	{
		await using var connection = new SqlConnection(TestConfiguration.From(Variable));

		await connection.OpenAsync();

		await using var command = new SqlCommand("SELECT user_access_desc FROM sys.databases WHERE name = @name", connection);

		command.Parameters.AddWithValue("@name", name);

		return await command.ExecuteScalarAsync() as string;
	}

	private static async Task<string?> State(string name)
	{
		await using var connection = new SqlConnection(TestConfiguration.From(Variable));

		await connection.OpenAsync();

		await using var command = new SqlCommand("SELECT state_desc FROM sys.databases WHERE name = @name", connection);

		command.Parameters.AddWithValue("@name", name);

		return await command.ExecuteScalarAsync() as string;
	}

	private static async Task<object?> Scalar(string text)
	{
		await using var connection = new SqlConnection(TestConfiguration.From(Variable));

		await connection.OpenAsync();

		await using var command = new SqlCommand(text, connection);

		return await command.ExecuteScalarAsync();
	}

	/// <remarks>
	/// Not pooled: the session that takes a database to single user keeps its one connection, and a pooled one would
	/// keep it after the statement.
	/// </remarks>
	private static async Task Run(string text)
	{
		await using var connection = new SqlConnection(new SqlConnectionStringBuilder(TestConfiguration.From(Variable)) { Pooling = false }.ConnectionString);

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
