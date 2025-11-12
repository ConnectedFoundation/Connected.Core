using Connected.Storage.Schemas;

namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Adds a primary key constraint to a PostgreSQL table column.
/// </summary>
/// <remarks>
/// This transaction generates and executes the ALTER TABLE ADD CONSTRAINT DDL statement to create
/// a primary key constraint on the specified column. PostgreSQL requires unique constraint names
/// within a schema, so the transaction uses the context's constraint name generation mechanism
/// to ensure uniqueness. The primary key constraint enforces uniqueness and non-nullability on
/// the column and creates an index automatically for efficient lookups. Multiple calls to add
/// primary keys on the same table will result in an error as PostgreSQL allows only one primary
/// key per table.
/// </remarks>
internal sealed class PrimaryKeyAdd(ISchemaColumn column)
	: ColumnTransaction(column)
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Generate a unique constraint name for the primary key
		 */
		var constraintName = Context.GenerateConstraintName(
			Context.Schema.Schema ?? "public",
			Context.Schema.Name,
			ConstraintNameType.PrimaryKey
		);

		/*
		 * Execute ALTER TABLE ADD CONSTRAINT PRIMARY KEY statement
		 */
		await Context.Execute($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)} ADD CONSTRAINT {Escape(constraintName)} PRIMARY KEY ({Escape(Column.Name)})");
	}
}
