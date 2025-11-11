using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

internal class TableAlter(ExistingSchema schema)
	: TableSynchronize
{
	protected override async Task OnExecute()
	{
		var dropped = new List<ObjectIndex>();

		foreach (var index in schema.Indexes)
		{
			if (!ColumnsMatched(index))
			{
				await new IndexDrop(index).Execute(Context);
				dropped.Add(index);
			}
		}

		foreach (var drop in dropped)
			schema.Indexes.Remove(drop);

		foreach (var existingColumn in schema.Columns)
		{
			if (Context.Schema.Columns.FirstOrDefault(f => string.Equals(f.Name, existingColumn.Name, StringComparison.OrdinalIgnoreCase)) is not ISchemaColumn column)
				await new ColumnDrop(existingColumn, schema).Execute(Context);
			else
				await new ColumnAlter(column, schema, existingColumn).Execute(Context);
		}

		var indexes = ParseIndexes(Context.Schema);

		foreach (var index in indexes)
		{
			if (!IndexExists(index))
				await new IndexCreate(index).Execute(Context);
		}
	}

	private bool IndexExists(IndexDescriptor index)
	{
		var existingIndexes = schema.Indexes.Where(f => f.Type != IndexType.PrimaryKey);

		foreach (var existingIndex in existingIndexes)
		{
			if (index.Unique && existingIndex.Type != IndexType.Unique)
				continue;

			if (!index.Unique && existingIndex.Type == IndexType.Unique)
				continue;

			var cols = index.Columns.OrderBy(f => f);
			var existingCols = existingIndex.Columns.OrderBy(f => f);

			if (cols.Count() != existingCols.Count())
				continue;

			for (var i = 0; i < cols.Count(); i++)
			{
				if (!string.Equals(cols.ElementAt(i), existingCols.ElementAt(i), StringComparison.OrdinalIgnoreCase))
					break;
			}

			return true;
		}

		return false;
	}

	private bool ColumnsMatched(ObjectIndex index)
	{
		if (index.Columns.Count == 1)
			return ColumnMatched(index);

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

		foreach (var column in Context.Schema.Columns)
		{
			if (string.Equals(column.Index, indexGroup, StringComparison.OrdinalIgnoreCase) && !columns.Contains(column) && column.IsIndex)
				columns.Add(column);
		}

		if (index.Columns.Count != columns.Count)
			return false;

		foreach (var column in columns.OrderBy(f => f.Name))
		{
			if (!index.Columns.Contains(column.Name, StringComparer.OrdinalIgnoreCase))
				return false;
		}

		return true;
	}

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
