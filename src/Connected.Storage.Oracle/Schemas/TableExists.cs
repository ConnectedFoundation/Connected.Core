namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Checks whether an Oracle database table exists.
/// </summary>
/// <remarks>
/// This query transaction executes a query against the ALL_TABLES system view to determine
/// whether a table with the specified name exists in the target schema (user). The operation
/// is used during schema synchronization to decide whether to create a new table or modify an
/// existing one. Oracle stores table metadata in ALL_TABLES (all accessible tables) or USER_TABLES
/// (current user's tables). Table names in Oracle are case-insensitive and stored in uppercase
/// unless quoted during creation. The query returns a boolean result indicating table existence,
/// enabling conditional table creation or modification operations based on the current database state.
/// </remarks>
internal sealed class TableExists
	: SynchronizationQuery<bool>
{
	/// <inheritdoc/>
	protected override async Task<bool> OnExecute()
	{
		/*
		 * Query ALL_TABLES to check for table existence.
		 * Oracle stores table information in ALL_TABLES system view.
		 * Table and schema names are converted to uppercase for comparison.
		 */
		var schema = string.IsNullOrWhiteSpace(Context.Schema.Schema)
			? Context.Schema.Schema
			: Context.Schema.Schema.ToUpperInvariant();

		var tableName = Context.Schema.Name.ToUpperInvariant();

		var rdr = await Context.OpenReader(new OracleStorageOperation
		{
			CommandText = string.IsNullOrWhiteSpace(schema)
				? $"SELECT 1 FROM ALL_TABLES WHERE TABLE_NAME = '{tableName}' AND OWNER = USER"
				: $"SELECT 1 FROM ALL_TABLES WHERE OWNER = '{schema}' AND TABLE_NAME = '{tableName}'"
		});

		var result = rdr.Read();

		rdr.Close();

		return result;
	}
}
