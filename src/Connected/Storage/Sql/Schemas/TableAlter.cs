using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Alters an existing database table to match the target schema definition.
/// </summary>
/// <remarks>
/// This transaction performs incremental alterations to an existing table structure without
/// requiring table recreation. It handles index modifications, column drops, and column alterations
/// in a coordinated sequence to ensure referential integrity is maintained. The operation first
/// identifies and drops indexes that no longer match the target schema, then processes column
/// changes, and finally creates new indexes. This approach minimizes disruption to existing data
/// while applying schema changes efficiently. The transaction intelligently compares existing and
/// target index definitions to avoid unnecessary drops and recreations of matching indexes.
/// </remarks>
internal class TableAlter(ExistingSchema schema)
	: TableSynchronize
{
	/// <inheritdoc/>
	protected override async Task OnExecute()
	{
		var dropped = new List<ObjectIndex>();

		/*
		 * Drop indexes that no longer match the target schema definition.
		 */
		foreach (var index in schema.Indexes)
		{
			if (!ColumnsMatched(index))
			{
				await new IndexDrop(index).Execute(Context);
				dropped.Add(index);
			}
		}

		/*
		 * Remove dropped indexes from the existing schema metadata.
		 */
		foreach (var drop in dropped)
			schema.Indexes.Remove(drop);

		/*
		 * Process each existing column: either drop it if not in target schema,
		 * or alter it to match the target definition.
		 */
		foreach (var existingColumn in schema.Columns)
		{
			if (Context.Schema.Columns.FirstOrDefault(f => string.Equals(f.Name, existingColumn.Name, StringComparison.OrdinalIgnoreCase)) is not ISchemaColumn column)
				await new ColumnDrop(existingColumn, schema).Execute(Context);
			else
				await new ColumnAlter(column, schema, existingColumn).Execute(Context);
		}

		/*
		 * Create new indexes that don't exist in the current schema.
		 */
		var indexes = ParseIndexes(Context.Schema);

		foreach (var index in indexes)
		{
			if (!IndexExists(index))
				await new IndexCreate(index).Execute(Context);
		}
	}

	/// <summary>
	/// Determines whether an index already exists in the existing schema.
	/// </summary>
	/// <param name="index">The index descriptor to check.</param>
	/// <returns><c>true</c> if a matching index exists; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// This method compares index uniqueness and column composition to determine if an
	/// equivalent index already exists, preventing duplicate index creation.
	/// </remarks>
	private bool IndexExists(IndexDescriptor index)
	{
		var existingIndexes = schema.Indexes.Where(f => f.Type != IndexType.PrimaryKey);

		/*
		 * Compare each existing index to determine if it matches the target index definition.
		 */
		foreach (var existingIndex in existingIndexes)
		{
			/*
			 * Skip indexes with mismatched uniqueness settings.
			 */
			if (index.Unique && existingIndex.Type != IndexType.Unique)
				continue;

			if (!index.Unique && existingIndex.Type == IndexType.Unique)
				continue;

			/*
			 * Sort columns for order-independent comparison.
			 */
			var cols = index.Columns.OrderBy(f => f);
			var existingCols = existingIndex.Columns.OrderBy(f => f);

			if (cols.Count() != existingCols.Count())
				continue;

			/*
			 * Compare column names element by element.
			 */
			for (var i = 0; i < cols.Count(); i++)
			{
				if (!string.Equals(cols.ElementAt(i), existingCols.ElementAt(i), StringComparison.OrdinalIgnoreCase))
					break;
			}

			return true;
		}

		return false;
	}

	/// <summary>
	/// Determines whether an index's columns still match the target schema definition.
	/// </summary>
	/// <param name="index">The index to check.</param>
	/// <returns><c>true</c> if the index columns still match; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// This method handles both single-column and multi-column indexes, verifying that all
	/// indexed columns exist in the target schema with appropriate index attributes.
	/// </remarks>
	private bool ColumnsMatched(ObjectIndex index)
	{
		/*
		 * Handle single-column indexes separately for simplified logic.
		 */
		if (index.Columns.Count == 1)
			return ColumnMatched(index);

		/*
		 * For multi-column indexes, verify all columns belong to the same named index group.
		 */
		var indexGroup = string.Empty;
		var columns = new List<ISchemaColumn>();

		foreach (var column in Context.Schema.Columns)
		{
			if (index.Columns.Contains(column.Name, StringComparer.OrdinalIgnoreCase))
			{
				if (string.IsNullOrWhiteSpace(column.Index))
					return false;

				if (string.IsNullOrWhiteSpace(indexGroup))
					indexGroup = column.Index;

				if (!string.Equals(indexGroup, column.Index, StringComparison.OrdinalIgnoreCase))
					return false;

				columns.Add(column);
			}
		}

		/*
		 * Collect any additional columns that are part of the same index group.
		 */
		foreach (var column in Context.Schema.Columns)
		{
			if (string.Equals(column.Index, indexGroup, StringComparison.OrdinalIgnoreCase) && !columns.Contains(column) && column.IsIndex)
				columns.Add(column);
		}

		/*
		 * Verify column count matches and all columns are present.
		 */
		if (index.Columns.Count != columns.Count)
			return false;

		foreach (var column in columns.OrderBy(f => f.Name))
		{
			if (!index.Columns.Contains(column.Name, StringComparer.OrdinalIgnoreCase))
				return false;
		}

		return true;
	}

	/// <summary>
	/// Determines whether a single-column index still matches the target schema definition.
	/// </summary>
	/// <param name="index">The single-column index to check.</param>
	/// <returns><c>true</c> if the column is still indexed with matching properties; otherwise, <c>false</c>.</returns>
	private bool ColumnMatched(ObjectIndex index)
	{
		if (Context.Schema.Columns.FirstOrDefault(f => string.Equals(f.Name, index.Columns[0], StringComparison.OrdinalIgnoreCase)) is not ISchemaColumn column)
			return false;

		if (!column.IsIndex)
			return false;

		if (index.Type == IndexType.Unique && !column.IsUnique)
			return false;

		if (index.Type == IndexType.Index && column.IsUnique)
			return false;

		return true;
	}
}
