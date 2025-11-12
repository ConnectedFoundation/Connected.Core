using System.Text;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Renames an Oracle database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the ALTER TABLE RENAME TO statement to rename
/// a table from a temporary name to its target name. Oracle uses ALTER TABLE old_name RENAME TO new_name
/// syntax instead of SQL Server's sp_rename procedure. The operation is primarily used during
/// table recreation scenarios where a temporary table is created, populated with data, and then
/// renamed to replace the original table. Oracle automatically commits DDL statements. The rename
/// operation preserves all table data, structure, indexes, and constraints. Note that Oracle
/// RENAME only changes the table name within the same schema; moving a table between schemas
/// requires different operations.
/// </remarks>
internal sealed class TableRename(string temporaryName)
	: TableTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute the ALTER TABLE RENAME TO statement for Oracle.
		 */
		await Context.Execute(CommandText);
	}

	/// <summary>
	/// Gets the DDL command text for renaming the table.
	/// </summary>
	/// <value>
	/// The ALTER TABLE RENAME TO statement with old and new names.
	/// </value>
	/// <remarks>
	/// Oracle uses ALTER TABLE old_name RENAME TO new_name syntax.
	/// Double quotes are used to preserve case sensitivity of identifiers.
	/// </remarks>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			/*
			 * Oracle RENAME syntax: ALTER TABLE old_name RENAME TO new_name
			 * Schema qualification is not needed in the RENAME TO clause
			 */
			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.Schema, temporaryName)} RENAME TO {Escape(Context.Schema.Name)}");

			return text.ToString();
		}
	}
}
