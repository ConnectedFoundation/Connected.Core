using Connected.Storage.Schemas;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Adds a primary key constraint to an Oracle table column.
/// </summary>
/// <remarks>
/// This transaction generates and executes the ALTER TABLE ADD CONSTRAINT DDL statement to create
/// a primary key constraint on the specified column. Oracle requires unique constraint names
/// within a schema, so the transaction uses the context's constraint name generation mechanism
/// to ensure uniqueness. The primary key constraint enforces uniqueness and non-nullability on
/// the column and creates a unique index automatically for efficient lookups. Multiple calls to add
/// primary keys on the same table will result in an error as Oracle allows only one primary
/// key per table. Constraint names are limited to 30 characters (pre-12.2) or 128 characters (12.2+)
/// and are case-insensitive (stored uppercase unless quoted).
/// </remarks>
internal sealed class PrimaryKeyAdd(ISchemaColumn column)
	: ColumnTransaction(column)
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Generate a unique constraint name for the primary key
		 * Oracle constraint names are limited to 30 characters (pre-12.2) or 128 characters (12.2+)
		 */
		var constraintName = Context.GenerateConstraintName(
			Context.Schema.Schema ?? string.Empty,
			Context.Schema.Name,
			ConstraintNameType.PrimaryKey
		);

		/*
		 * Execute ALTER TABLE ADD CONSTRAINT PRIMARY KEY statement
		 * Oracle syntax uses double-quoted identifiers for case-sensitive names
		 */
		await Context.Execute($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)} ADD CONSTRAINT {Escape(constraintName)} PRIMARY KEY ({Escape(Column.Name)})");
	}
}
