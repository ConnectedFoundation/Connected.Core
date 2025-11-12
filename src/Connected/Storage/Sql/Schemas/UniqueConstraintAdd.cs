using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Adds a unique constraint to a database table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the ALTER TABLE ADD CONSTRAINT DDL statement to
/// add a unique constraint to one or more columns. Unique constraints ensure that all values
/// in the specified columns are distinct across rows in the table, preventing duplicate entries.
/// The constraint is created as a non-clustered index by default with ascending sort order.
/// Constraint names are automatically generated using a consistent naming convention. The index
/// is created on the specified filegroup for optimal storage allocation. This operation is used
/// when index descriptors are marked as unique, providing both uniqueness enforcement and
/// query performance benefits.
/// </remarks>
internal class UniqueConstraintAdd(IndexDescriptor index)
	: TableTransaction
{
	/// <summary>
	/// Gets the index descriptor defining the unique constraint.
	/// </summary>
	private IndexDescriptor Index { get; } = index;

	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Execute the ALTER TABLE ADD CONSTRAINT UNIQUE statement.
		 */
		await Context.Execute(CommandText);
	}

	/// <summary>
	/// Gets the DDL command text for adding the unique constraint.
	/// </summary>
	/// <value>
	/// The ALTER TABLE ADD CONSTRAINT UNIQUE statement with column specifications.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)}");
			text.AppendLine($"ADD CONSTRAINT [{Context.GenerateConstraintName(Context.Schema.Schema, Context.Schema.Name, ConstraintNameType.Index)}] UNIQUE NONCLUSTERED (");
			var comma = string.Empty;

			/*
			 * Add each column to the unique constraint definition with ascending sort order.
			 */
			foreach (var column in Index.Columns)
			{
				text.AppendLine($"{comma}{Escape(column)} ASC");

				comma = ",";
			}

			/*
			 * Specify the filegroup where the constraint index will be stored.
			 */
			text.AppendLine($") ON {Escape(SchemaExtensions.FileGroup)}");

			return text.ToString();
		}
	}
}
