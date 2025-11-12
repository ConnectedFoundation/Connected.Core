using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Drops a constraint from a database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the ALTER TABLE DROP CONSTRAINT DDL statement
/// to remove a named constraint from a table. It is typically used to drop index constraints,
/// unique constraints, or other table-level constraints during schema synchronization operations.
/// The operation requires the constraint name to be known and properly referenced in the index
/// descriptor.
/// </remarks>
internal class ConstraintDrop(ObjectIndex index)
	: TableTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute the ALTER TABLE DROP CONSTRAINT statement.
		 */
		await Context.Execute(CommandText);
	}

	/// <summary>
	/// Gets the DDL command text for dropping the constraint.
	/// </summary>
	/// <value>
	/// The ALTER TABLE DROP CONSTRAINT statement with the constraint name.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)} DROP CONSTRAINT {index.Name};");

			return text.ToString();
		}
	}
}
