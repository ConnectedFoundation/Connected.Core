namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Checks whether a PostgreSQL database table exists.
/// </summary>
/// <remarks>
/// This query transaction executes a query against the information_schema.tables system view
/// to determine whether a table with the specified name exists in the target schema. The operation
/// is used during schema synchronization to decide whether to create a new table or modify an
/// existing one. The query returns a boolean result indicating table existence, enabling conditional
/// table creation or modification operations based on the current database state.
/// </remarks>
internal sealed class TableExists
	: SynchronizationQuery<bool>
{
	/// <inheritdoc/>
	protected override async Task<bool> OnExecute()
	{
		/*
		 * Query information_schema.tables to check for table existence.
		 * PostgreSQL stores table information in the information_schema.
		 */
		var schema = string.IsNullOrWhiteSpace(Context.Schema.Schema) ? "public" : Context.Schema.Schema;

		var rdr = await Context.OpenReader(new PostgreSqlStorageOperation
		{
			CommandText = $"SELECT 1 FROM information_schema.tables WHERE table_schema = '{schema}' AND table_name = '{Context.Schema.Name}'"
		});

		var result = rdr.Read();

		rdr.Close();

		return result;
	}
}
