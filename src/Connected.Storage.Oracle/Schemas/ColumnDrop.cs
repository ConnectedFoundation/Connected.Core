using Connected.Storage.Schemas;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Drops a column from an Oracle database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the ALTER TABLE DROP COLUMN DDL statement to remove
/// a column from a table. The operation permanently deletes the column and all its data from the
/// table. Oracle automatically drops dependent objects like indexes when a column is dropped,
/// but constraints may need explicit CASCADE CONSTRAINTS option. This implementation uses a
/// conservative approach without CASCADE to prevent accidental data loss. The operation should
/// be used with caution as it is irreversible and results in permanent data loss. For large
/// tables, consider using ALTER TABLE SET UNUSED and DROP UNUSED COLUMNS for better performance.
/// </remarks>
internal sealed class ColumnDrop(ISchemaColumn column)
	: ColumnTransaction(column)
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute ALTER TABLE DROP COLUMN statement.
		 * Oracle automatically drops dependent indexes but not constraints.
		 * Using conservative approach without CASCADE CONSTRAINTS.
		 */
		await Context.Execute($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)} DROP COLUMN {Escape(Column.Name)}");
	}
}
