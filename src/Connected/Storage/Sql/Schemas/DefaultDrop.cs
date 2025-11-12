using Connected.Storage.Schemas;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Drops a default constraint from a database column.
/// </summary>
/// <remarks>
/// This transaction generates and executes the ALTER TABLE DROP CONSTRAINT DDL statement
/// to remove a default constraint from a column. The operation first queries the existing
/// schema metadata to locate the default constraint name associated with the column, as
/// default constraints are named objects in SQL databases. If no default constraint is found,
/// the operation is skipped. This is commonly used before altering a column's data type or
/// when updating a column's default value.
/// </remarks>
internal class DefaultDrop(ISchemaColumn column)
	: ColumnTransaction(column)
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Skip the operation if no default constraint name is found for this column.
		 */
		if (string.IsNullOrWhiteSpace(DefaultName))
			return;

		/*
		 * Execute the ALTER TABLE DROP CONSTRAINT statement.
		 */
		await Context.Execute(CommandText);
	}

	/// <summary>
	/// Gets the DDL command text for dropping the default constraint.
	/// </summary>
	/// <value>
	/// The ALTER TABLE DROP CONSTRAINT statement with the constraint name.
	/// </value>
	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)}");
			text.AppendLine($"DROP CONSTRAINT {DefaultName};");

			return text.ToString();
		}
	}

	/// <summary>
	/// Gets the name of the default constraint for the column.
	/// </summary>
	/// <value>
	/// The constraint name if found; otherwise, <c>null</c>.
	/// </value>
	/// <remarks>
	/// This property searches the existing schema descriptor for a default constraint
	/// that is associated with the current column by matching constraint type and column name.
	/// </remarks>
	private string? DefaultName
	{
		get
		{
			if (Context.ExistingSchema?.Descriptor is null)
				return null;

			/*
			 * Search through all constraints to find the default constraint for this column.
			 */
			foreach (var constraint in Context.ExistingSchema.Descriptor.Constraints)
			{
				if (constraint.ConstraintType == ConstraintType.Default)
				{
					if (constraint.Columns.Count == 1 && string.Equals(constraint.Columns[0], Column.Name, StringComparison.OrdinalIgnoreCase))
						return constraint.Name;
				}
			}

			return null;
		}
	}
}
