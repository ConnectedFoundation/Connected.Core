namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Synchronizes a database schema namespace by creating it if it doesn't exist.
/// </summary>
/// <remarks>
/// This transaction ensures that the schema namespace exists in the database before table
/// synchronization operations are performed. It checks for schema existence and creates the
/// schema if needed. Empty or whitespace schema names are ignored as they represent the default
/// schema which always exists. This operation is executed before table synchronization to ensure
/// all schema objects can be created in their designated namespace without encountering schema
/// not found errors.
/// </remarks>
internal class SchemaSynchronize
	: SynchronizationTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Skip schema creation for empty or whitespace schema names (default schema).
		 */
		if (string.IsNullOrWhiteSpace(Context.Schema.Schema))
			return;

		/*
		 * Check if the schema exists and create it if it doesn't.
		 */
		if (!await new SchemaExists(Context.Schema.Schema).Execute(Context))
			await CreateSchema();
	}

	/// <summary>
	/// Creates a new database schema namespace.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task CreateSchema()
	{
		await Context.Execute($"CREATE SCHEMA {Context.Schema.Schema};");
	}
}
