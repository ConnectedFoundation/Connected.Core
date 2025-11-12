using Connected.Storage.Schemas;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Drops a column from an existing database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the ALTER TABLE DROP COLUMN DDL statement to remove
/// a column from a table. Before dropping the column, it handles cleanup of dependent objects
/// including default constraints and indexes that reference the column. This ensures that the
/// column can be dropped without violating referential integrity constraints. The operation
/// follows a specific sequence: drop defaults, drop indexes, then drop the column itself.
/// </remarks>
internal class ColumnDrop(ISchemaColumn column, ExistingSchema existing)
	: ColumnTransaction(column)
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Drop any default constraint associated with the column before dropping the column.
		 */
		if (!string.IsNullOrWhiteSpace(Column.DefaultValue))
			await new DefaultDrop(Column).Execute(Context);

		/*
		 * Resolve and drop all indexes that reference this column to avoid
		 * constraint violations when the column is dropped.
		 */
		var indexes = existing.ResolveIndexes(Column.Name);

		foreach (var index in indexes)
			await new IndexDrop(index).Execute(Context);

		/*
		 * Execute the ALTER TABLE DROP COLUMN statement.
		 */
		await Context.Execute(CommandText);
	}

	/// <summary>
	/// Gets the DDL command text for dropping the column.
	/// </summary>
	/// <value>
	/// The ALTER TABLE DROP COLUMN statement.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)} DROP COLUMN {Escape(Column.Name)};");

			return text.ToString();
		}
	}
}