using Connected.Configuration;
using Connected.Storage.Containers;
using Microsoft.Data.SqlClient;

namespace Connected.Storage.Sql.Containers;

/// <summary>
/// Manages SQL Server storage containers: each container is a database on the default database's server.
/// </summary>
internal sealed class SqlStorageContainerProvider(IConfigurationService configuration)
	: StorageContainerProvider(configuration)
{
	/// <inheritdoc/>
	public override Type ConnectionType => typeof(SqlDataConnection);

	/// <inheritdoc/>
	protected override string DefaultName() => new SqlConnectionStringBuilder(DefaultConnectionString).InitialCatalog;

	/// <inheritdoc/>
	protected override async Task<string> OnCreate(string name)
	{
		await Execute($"IF DB_ID(@name) IS NULL CREATE DATABASE {Quote(name)}", name);

		return new SqlConnectionStringBuilder(DefaultConnectionString) { InitialCatalog = name }.ConnectionString;
	}

	/// <inheritdoc/>
	protected override async Task OnDrop(string name)
	{
		/*
		 * Pooled connections would keep the database in use; open sessions are rolled back so the drop cannot wait.
		 */
		SqlConnection.ClearPool(new SqlConnection(new SqlConnectionStringBuilder(DefaultConnectionString) { InitialCatalog = name }.ConnectionString));

		await Execute($"""
			IF DB_ID(@name) IS NOT NULL
			BEGIN
				ALTER DATABASE {Quote(name)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
				DROP DATABASE {Quote(name)};
			END
			""", name);
	}

	private async Task Execute(string text, string name)
	{
		await using var connection = new SqlConnection(new SqlConnectionStringBuilder(DefaultConnectionString) { InitialCatalog = "master" }.ConnectionString);

		await connection.OpenAsync(CancellationToken);

		await using var command = new SqlCommand(text, connection);

		command.Parameters.AddWithValue("@name", name);
		await command.ExecuteNonQueryAsync(CancellationToken);
	}

	private static string Quote(string name) => $"[{name.Replace("]", "]]")}]";
}
