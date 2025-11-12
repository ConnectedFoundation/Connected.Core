namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Checks whether a database schema exists.
/// </summary>
/// <remarks>
/// This query transaction executes a query against the INFORMATION_SCHEMA.SCHEMATA system view
/// to determine whether a schema with the specified name exists in the database. The operation
/// is used during schema synchronization to decide whether to create a new schema namespace or
/// use an existing one. Empty or whitespace schema names are treated as existing to support
/// default schema scenarios. The query returns a boolean result indicating schema existence,
/// enabling conditional schema creation operations based on the current database state.
/// </remarks>
internal class SchemaExists(string name)
	: SynchronizationQuery<bool>
{
	/// <inheritdoc/>
	protected override async Task<bool> OnExecute()
	{
		/*
		 * Empty or whitespace schema names are treated as existing (default schema).
		 */
		if (string.IsNullOrWhiteSpace(name))
			return true;

		/*
		 * Query INFORMATION_SCHEMA.SCHEMATA to check for schema existence.
		 */
		var rdr = await Context.OpenReader(new SqlStorageOperation { CommandText = $"SELECT * FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{name}'" });
		var result = rdr.Read();

		rdr.Close();

		return result;
	}
}
