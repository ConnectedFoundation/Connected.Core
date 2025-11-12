using System.Text;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Drops an Oracle database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the DROP TABLE DDL statement to remove a table
/// from the Oracle database. This operation permanently deletes the table structure and all data
/// contained within it. The operation should be used with caution as it is irreversible and
/// automatically commits in Oracle (DDL auto-commit). It is typically used during table recreation
/// scenarios where the table is dropped and then recreated with a new structure, or when cleaning
/// up obsolete tables during schema migrations. Oracle DROP TABLE also removes all associated
/// indexes, triggers, and constraints. The CASCADE CONSTRAINTS option can be used to drop dependent
/// foreign key constraints, but this implementation uses the default behavior requiring manual
/// constraint cleanup beforehand.
/// </remarks>
internal sealed class TableDrop
	: TableTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute the DROP TABLE statement to remove the table.
		 * Oracle automatically commits DDL statements.
		 */
		await Context.Execute(CommandText);
	}

	/// <summary>
	/// Gets the DDL command text for dropping the table.
	/// </summary>
	/// <value>
	/// The DROP TABLE statement with double-quoted schema and table name.
	/// </value>
	/// <remarks>
	/// Oracle uses double-quoted identifiers to preserve case sensitivity.
	/// The statement does not include CASCADE CONSTRAINTS to prevent accidental
	/// deletion of dependent objects. If cascade deletion is needed, foreign key
	/// constraints should be explicitly dropped first.
	/// </remarks>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			/*
			 * Generate DROP TABLE with Oracle-specific double-quote escaping
			 */
			text.AppendLine($"DROP TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)}");

			return text.ToString();
		}
	}
}
