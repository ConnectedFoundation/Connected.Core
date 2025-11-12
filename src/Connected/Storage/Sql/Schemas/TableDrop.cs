using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Drops a database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the DROP TABLE DDL statement to remove a table
/// from the database. This operation permanently deletes the table structure and all data
/// contained within it. The operation should be used with caution as it is irreversible.
/// It is typically used during table recreation scenarios where the table is dropped and
/// then recreated with a new structure, or when cleaning up obsolete tables during schema
/// migrations.
/// </remarks>
internal class TableDrop
	: TableTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute the DROP TABLE statement to remove the table.
		 */
		await Context.Execute(CommandText);
	}

	/// <summary>
	/// Gets the DDL command text for dropping the table.
	/// </summary>
	/// <value>
	/// The DROP TABLE statement with schema and table name.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"DROP TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)}");

			return text.ToString();
		}
	}
}
