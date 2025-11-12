namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Checks whether a PostgreSQL database schema exists.
/// </summary>
/// <remarks>
/// This query transaction executes a query against the pg_catalog.pg_namespace system catalog
/// to determine whether a schema with the specified name exists in the database. The operation
/// is used during schema synchronization to decide whether to create a new schema namespace or
/// use an existing one. Empty or whitespace schema names are treated as existing to support
/// default schema scenarios. The query returns a boolean result indicating schema existence,
/// enabling conditional schema creation operations based on the current database state.
/// PostgreSQL stores schema information in the pg_namespace catalog table rather than
/// INFORMATION_SCHEMA for more reliable and efficient lookups.
/// </remarks>
internal sealed class SchemaExists(string name)
	: SynchronizationQuery<bool>
{
	/// <inheritdoc/>
	protected override async Task<bool> OnExecute()
	{
		/*
		 * Empty or whitespace schema names are treated as existing (default public schema).
		 */
		if (string.IsNullOrWhiteSpace(name))
			return true;

		/*
		 * Query pg_catalog.pg_namespace to check for schema existence.
		 * Using pg_catalog provides better performance than INFORMATION_SCHEMA.
		 */
		var rdr = await Context.OpenReader(new PostgreSqlStorageOperation
		{
			CommandText = $"SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname = '{name}'"
		});

		var result = rdr.Read();

		rdr.Close();

		return result;
	}
}
