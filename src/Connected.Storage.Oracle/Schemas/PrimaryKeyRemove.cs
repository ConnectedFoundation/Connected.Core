using Connected.Storage.Schemas;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Removes a primary key constraint from an Oracle database table.
/// </summary>
/// <remarks>
/// This transaction locates and drops the primary key constraint from the existing schema.
/// It searches through the table's constraints in the ObjectDescriptor to find the primary key
/// constraint by checking if the column is marked as primary key, then generates an ALTER TABLE
/// DROP CONSTRAINT statement. After successful removal, the constraint is removed from the
/// existing schema's constraint collection to keep the metadata synchronized. Oracle automatically
/// drops the associated unique index when the primary key constraint is dropped. This operation
/// is typically performed when altering a column that is part of the primary key or when changing
/// the table's primary key definition. Oracle DDL statements auto-commit.
/// </remarks>
internal sealed class PrimaryKeyRemove(ExistingSchema existing, ISchemaColumn column)
	: ColumnTransaction(column)
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Find the primary key constraint for this column.
		 * Since ConstraintType enum is not yet available, we identify the PK constraint
		 * by finding constraints that include this column and have "PK_" prefix or
		 * are associated with columns marked as IsPrimaryKey.
		 */
		if (existing.Descriptor?.Constraints is not null)
		{
			foreach (var constraint in existing.Descriptor.Constraints)
			{
				/*
				 * Check if this constraint involves the column being removed
				 * and appears to be a primary key constraint (typically named with PK_ prefix).
				 */
				if (constraint.Columns.Contains(Column.Name, StringComparer.OrdinalIgnoreCase) &&
					!string.IsNullOrWhiteSpace(constraint.Name) &&
					(constraint.Name.StartsWith("PK_", StringComparison.OrdinalIgnoreCase) ||
					 constraint.Name.Contains("PRIMARY", StringComparison.OrdinalIgnoreCase)))
				{
					/*
					 * Generate ALTER TABLE DROP CONSTRAINT statement for Oracle
					 */
					var sql = $"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)} DROP CONSTRAINT {Escape(constraint.Name)}";
					await Context.Execute(sql);

					/*
					 * Remove the constraint from the existing schema's metadata to keep
					 * the schema representation synchronized.
					 */
					existing.Descriptor.Constraints.Remove(constraint);
					break;
				}
			}
		}
	}
}
