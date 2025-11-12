using Connected.Storage.Schemas;

namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Drops a column from a PostgreSQL database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the ALTER TABLE DROP COLUMN DDL statement to remove
/// a column from a table. The operation permanently deletes the column and all its data from the
/// table. PostgreSQL supports CASCADE option to automatically drop dependent objects, but this
/// implementation uses the default RESTRICT behavior which fails if dependencies exist. This
/// conservative approach prevents accidental data loss from cascade deletions. The operation
/// should be used with caution as it is irreversible and results in data loss.
/// </remarks>
internal sealed class ColumnDrop(ISchemaColumn column)
	: ColumnTransaction(column)
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute ALTER TABLE DROP COLUMN statement.
		 * Using RESTRICT (default) to prevent accidental cascade deletions.
		 */
		await Context.Execute($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)} DROP COLUMN {Escape(Column.Name)} RESTRICT");
	}
}
