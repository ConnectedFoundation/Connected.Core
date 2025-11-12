namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Checks whether an Oracle database schema exists.
/// </summary>
/// <remarks>
/// This query transaction executes a query against the ALL_USERS or DBA_USERS system catalog
/// to determine whether a schema (user) with the specified name exists in the database. In Oracle,
/// schemas are synonymous with users, so checking for schema existence means checking for user
/// existence. The operation is used during schema synchronization to decide whether to create a
/// new schema or use an existing one. Empty or whitespace schema names are treated as existing to
/// support default schema scenarios (current user schema). The query returns a boolean result
/// indicating schema existence, enabling conditional schema creation operations based on the current
/// database state. Oracle stores user/schema information in the ALL_USERS or USER_USERS views rather
/// than PostgreSQL's pg_namespace or SQL Server's sys.schemas for reliable schema lookups.
/// </remarks>
internal sealed class SchemaExists(string name)
	: SynchronizationQuery<bool>
{
	/// <inheritdoc/>
	protected override async Task<bool> OnExecute()
	{
		/*
		 * Empty or whitespace schema names are treated as existing (default current user schema).
		 */
		if (string.IsNullOrWhiteSpace(name))
			return true;

		/*
		 * Query ALL_USERS to check for schema/user existence.
		 * Oracle treats schemas and users as synonymous concepts.
		 * Using ALL_USERS provides reliable lookup without requiring DBA privileges.
		 */
		var rdr = await Context.OpenReader(new OracleStorageOperation
		{
			CommandText = $"SELECT 1 FROM ALL_USERS WHERE USERNAME = '{name.ToUpper()}'"
		});

		var result = rdr.Read();

		rdr.Close();

		return result;
	}
}
