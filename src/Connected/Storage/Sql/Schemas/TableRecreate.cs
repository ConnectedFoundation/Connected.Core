namespace Connected.Storage.Sql.Schemas;

internal class TableRecreate(ExistingSchema existing)
	: TableTransaction
{
	protected override async Task OnExecute()
	{
		var add = new TableCreate(true);

		await add.Execute(Context);

		await ExecuteDefaults(add.TemporaryName ?? throw new NullReferenceException(SR.ErrExpectedTemporaryName));

		if (HasIdentity)
			await new IdentityInsert(add.TemporaryName, true).Execute(Context);

		await new DataCopy(existing, add.TemporaryName).Execute(Context);

		if (HasIdentity)
			await new IdentityInsert(add.TemporaryName, false).Execute(Context);

		await new TableDrop().Execute(Context);
		await new TableRename(add.TemporaryName).Execute(Context);

		await ExecutePrimaryKey();
		await ExecuteIndexes();
	}

	private bool HasIdentity
	{
		get
		{
			foreach (var column in Context.Schema.Columns)
			{
				if (column.IsPrimaryKey && column.IsIdentity)
					return true;
			}

			return false;
		}
	}

	private async Task ExecutePrimaryKey()
	{
		var pk = Context.Schema.Columns.FirstOrDefault(f => f.IsPrimaryKey);

		if (pk != null)
			await new PrimaryKeyAdd(pk).Execute(Context);
	}

	private async Task ExecuteDefaults(string tableName)
	{
		foreach (var column in Context.Schema.Columns)
		{
			if (!string.IsNullOrWhiteSpace(column.DefaultValue))
				await new DefaultAdd(column, tableName).Execute(Context);
		}
	}

	private async Task ExecuteIndexes()
	{
		var indexes = ParseIndexes(Context.Schema);

		foreach (var index in indexes)
			await new IndexCreate(index).Execute(Context);
	}
}
