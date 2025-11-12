using System.Text;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Drops an index from an Oracle database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the appropriate DDL statement to remove an index
/// from a table. Oracle uses simple DROP INDEX syntax for both regular and unique indexes.
/// Primary key indexes cannot be dropped directly as they are part of the primary key constraint
/// and must be dropped by dropping the constraint itself using ALTER TABLE DROP CONSTRAINT.
/// Oracle automatically commits DDL statements. The operation removes the index structure and
/// frees the associated storage space. Function-based indexes, bitmap indexes, and B-tree indexes
/// all use the same DROP INDEX syntax in Oracle.
/// </remarks>
internal sealed class IndexDrop(ObjectIndex index)
	: TableTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute DROP INDEX statement for Oracle.
		 * Oracle uses simple DROP INDEX syntax for all index types except primary keys.
		 */
		await Context.Execute(CommandText);
	}

	/// <summary>
	/// Gets the DDL command text for dropping the index.
	/// </summary>
	/// <value>
	/// The DROP INDEX statement with double-quoted schema and index name.
	/// </value>
	/// <remarks>
	/// Oracle DROP INDEX syntax is simpler than SQL Server - just DROP INDEX schema.index_name.
	/// No need to specify the table name in Oracle.
	/// </remarks>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			/*
			 * Oracle DROP INDEX syntax: DROP INDEX schema.index_name
			 * No need to specify ON table_name like SQL Server
			 */
			if (!string.IsNullOrWhiteSpace(Context.Schema.Schema))
				text.AppendLine($"DROP INDEX {Escape(Context.Schema.Schema, index.Name)}");
			else
				text.AppendLine($"DROP INDEX {Escape(index.Name)}");

			return text.ToString();
		}
	}
}
