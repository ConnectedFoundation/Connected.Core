using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Creates a database index on a table.
/// </summary>
/// <remarks>
/// This transaction generates and executes the CREATE INDEX DDL statement to add an index
/// to a table. If the index is marked as unique, it delegates to the UniqueConstraintAdd
/// operation instead. The operation creates non-clustered indexes by default with ascending
/// sort order on all indexed columns. The index is created on the specified filegroup for
/// optimal storage allocation. Index names are automatically generated using a consistent
/// naming convention to avoid conflicts.
/// </remarks>
internal class IndexCreate(IndexDescriptor index)
	: TableTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * If the index requires uniqueness, delegate to unique constraint creation
		 * instead of creating a regular index.
		 */
		if (index.Unique)
			await new UniqueConstraintAdd(index).Execute(Context);
		else
			await Context.Execute(CommandText);
	}

	/// <summary>
	/// Gets the DDL command text for creating the index.
	/// </summary>
	/// <value>
	/// The CREATE NONCLUSTERED INDEX statement with column specifications.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"CREATE NONCLUSTERED INDEX [{Context.GenerateConstraintName(Context.Schema.Schema, Context.Schema.Name, ConstraintNameType.Index)}] ON {Escape(Context.Schema.Schema, Context.Schema.Name)}(");
			var comma = string.Empty;

			/*
			 * Add each column to the index definition with ascending sort order.
			 */
			foreach (var column in index.Columns)
			{
				text.AppendLine($"{comma}{Escape(column)} ASC");

				comma = ",";
			}

			/*
			 * Specify the filegroup where the index will be stored.
			 */
			text.AppendLine($") ON {Escape(SchemaExtensions.FileGroup)}");

			return text.ToString();
		}
	}
}
