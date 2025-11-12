namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Alters an existing Oracle table to match the desired schema definition.
/// </summary>
/// <remarks>
/// This transaction compares the existing table structure with the desired schema and performs
/// necessary modifications including adding new columns, altering existing columns, and dropping
/// columns that are no longer defined in the schema. The operation uses <see cref="ColumnComparer"/>
/// to determine differences between existing and desired columns, then delegates to specialized
/// transactions (<see cref="ColumnAdd"/>, <see cref="ColumnAlter"/>, <see cref="ColumnDrop"/>) to
/// apply the changes. Oracle's ALTER TABLE statement supports multiple ADD, MODIFY, and DROP clauses
/// but this implementation uses separate statements for better error handling and granular control.
/// The transaction ensures that table structure stays synchronized with entity definitions while
/// preserving existing data where possible.
/// </remarks>
internal sealed class TableAlter
	: TableTransaction
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		if (Context.ExistingSchema?.Descriptor is null)
			return;

		/*
		 * Compare existing columns with desired schema columns
		 */
		foreach (var column in Context.Schema.Columns)
		{
			var existing = Context.ExistingSchema.Columns.FirstOrDefault(f => string.Equals(f.Name, column.Name, StringComparison.OrdinalIgnoreCase));

			if (existing is null)
			{
				/*
				 * Column doesn't exist, add it
				 */
				await new ColumnAdd(column).Execute(Context);
			}
			else if (!ColumnComparer.Equals(column, existing))
			{
				/*
				 * Column exists but definition differs, alter it
				 */
				await new ColumnAlter(column).Execute(Context);
			}
		}

		/*
		 * Drop columns that no longer exist in the desired schema
		 */
		foreach (var existing in Context.ExistingSchema.Columns)
		{
			if (!Context.Schema.Columns.Any(a => string.Equals(a.Name, existing.Name, StringComparison.OrdinalIgnoreCase)))
				await new ColumnDrop(existing).Execute(Context);
		}
	}
}
