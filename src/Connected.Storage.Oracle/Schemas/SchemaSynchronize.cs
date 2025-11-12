namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Synchronizes an Oracle database schema (user).
/// </summary>
/// <remarks>
/// This transaction ensures that the schema (user) specified in the schema definition exists
/// in the Oracle database. In Oracle, schemas and users are synonymous - a schema is automatically
/// created when a user is created. Unlike PostgreSQL's CREATE SCHEMA, Oracle requires CREATE USER
/// with authentication credentials. Since this is a security-sensitive operation that requires
/// administrative privileges and passwords, this implementation does NOT automatically create users.
/// Instead, it verifies that the schema/user exists and throws an exception if it doesn't. Database
/// administrators must create users manually using SQL*Plus or other tools with proper authentication.
/// Empty or whitespace schema names are treated as the current user schema and validation is skipped.
/// </remarks>
internal sealed class SchemaSynchronize
	: SynchronizationTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Check if the schema (user) already exists in the database.
		 * Query ALL_USERS to verify the schema exists.
		 */
		if (await new SchemaExists(Context.Schema.Schema).Execute(Context))
			return;

		/*
		 * Schema (user) doesn't exist.
		 * Unlike PostgreSQL, Oracle does not support CREATE SCHEMA without CREATE USER.
		 * Creating users requires administrative privileges and authentication credentials.
		 * This must be done manually by a DBA.
		 */
		throw new InvalidOperationException(
			$"Oracle schema (user) '{Context.Schema.Schema}' does not exist. " +
			"Oracle schemas are synonymous with users and must be created manually by a database administrator " +
			"using CREATE USER statements with appropriate passwords and privileges. " +
			"Example: CREATE USER {Context.Schema.Schema} IDENTIFIED BY password; " +
			"GRANT CONNECT, RESOURCE TO {Context.Schema.Schema};");
	}
}
