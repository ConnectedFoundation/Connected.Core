namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Synchronizes a PostgreSQL database schema namespace.
/// </summary>
/// <remarks>
/// This transaction ensures that the schema namespace specified in the schema definition exists
/// in the PostgreSQL database. If the schema doesn't exist, it creates it using the CREATE SCHEMA
/// statement. The operation is fundamental to schema synchronization as all database objects must
/// reside within a schema namespace. Empty or whitespace schema names are treated as the default
/// public schema and no action is taken. This transaction must execute before any table or object
/// synchronization to ensure the target schema exists.
/// </remarks>
internal sealed class SchemaSynchronize
	: SynchronizationTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Check if the schema already exists in the database.
		 */
		if (await new SchemaExists(Context.Schema.Schema).Execute(Context))
			return;

		/*
		 * Create the schema if it doesn't exist.
		 * PostgreSQL uses CREATE SCHEMA syntax.
		 */
		await Context.Execute($"CREATE SCHEMA {Escape(Context.Schema.Schema)}");
	}
}
