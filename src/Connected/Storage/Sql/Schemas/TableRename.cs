using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Renames a database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the sp_rename system stored procedure to rename
/// a table from a temporary name to its target name. The operation is primarily used during
/// table recreation scenarios where a temporary table is created, populated with data, and then
/// renamed to replace the original table. The sp_rename procedure updates the table name in
/// the database system catalogs while preserving all table data and structure.
/// </remarks>
internal class TableRename(string temporaryName)
	: TableTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute the sp_rename stored procedure to rename the table.
		 */
		await Context.Execute(CommandText);
	}

	/// <summary>
	/// Gets the DDL command text for renaming the table.
	/// </summary>
	/// <value>
	/// The EXECUTE sp_rename statement with source and target names.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"EXECUTE sp_rename N'{Unescape(Context.Schema.Schema)}.{Unescape(temporaryName)}', N'{Unescape(Context.Schema.Name)}', 'OBJECT'");

			return text.ToString();
		}
	}
}
