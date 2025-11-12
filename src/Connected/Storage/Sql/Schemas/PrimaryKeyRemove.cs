using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Removes a primary key constraint from a database table.
/// </summary>
/// <remarks>
/// This transaction locates and drops the primary key constraint from the existing schema.
/// It searches through the table's indexes to find the primary key constraint and delegates
/// to the ConstraintDrop operation to remove it. After successful removal, the constraint
/// is also removed from the existing schema's index collection to keep the metadata synchronized.
/// This operation is typically performed when altering a column that is part of the primary key
/// or when changing the table's primary key definition.
/// </remarks>
internal class PrimaryKeyRemove(ExistingSchema existing, ISchemaColumn column)
	: ColumnTransaction(column)
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		/*
		 * Locate the primary key constraint in the existing schema.
		 */
		if (existing.Indexes.FirstOrDefault(f => f.Type == IndexType.PrimaryKey) is ObjectIndex constraint)
		{
			/*
			 * Drop the primary key constraint.
			 */
			await new ConstraintDrop(constraint).Execute(Context);

			/*
			 * Remove the constraint from the existing schema's metadata to keep
			 * the schema representation synchronized.
			 */
			existing.Indexes.Remove(constraint);
		}
	}
}
