using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Drops an index from a database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the appropriate DDL statement to remove an index
/// from a table based on its type. Regular indexes are dropped using DROP INDEX statements,
/// while unique indexes and primary key indexes are dropped using ALTER TABLE DROP CONSTRAINT
/// since they are implemented as constraints. The operation intelligently determines the
/// correct approach based on the index type to ensure proper cleanup without constraint
/// violations.
/// </remarks>
internal class IndexDrop(ObjectIndex index)
	: TableTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Choose the appropriate drop strategy based on the index type.
		 */
		switch (index.Type)
		{
			case IndexType.Index:
				/*
				 * Regular indexes are dropped directly using DROP INDEX.
				 */
				await Context.Execute(CommandText);
				break;

			case IndexType.Unique:
			case IndexType.PrimaryKey:
				/*
				 * Unique and primary key indexes are constraints and must be dropped
				 * using ALTER TABLE DROP CONSTRAINT.
				 */
				await new ConstraintDrop(index).Execute(Context);
				break;
		}
	}

	/// <summary>
	/// Gets the DDL command text for dropping the index.
	/// </summary>
	/// <value>
	/// The DROP INDEX statement with index and table names.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"DROP INDEX {index.Name} ON {Escape(Context.Schema.Schema, Context.Schema.Name)};");

			return text.ToString();
		}
	}
}