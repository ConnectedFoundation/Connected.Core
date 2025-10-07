using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class TableCreate : TableTransaction
{
	public TableCreate(bool temporary)
	{
		Temporary = temporary;

		if (Temporary)
			TemporaryName = $"T{Guid.NewGuid().ToString().Replace("-", string.Empty)}";
	}

	private bool Temporary { get; }

	public string TemporaryName { get; }

	protected override async Task OnExecute()
	{
		await Context.Execute(CommandText);

		if (!Temporary)
		{
			await ExecutePrimaryKey();
			await ExecuteDefaults();
			await ExecuteIndexes();
		}
	}

	private async Task ExecutePrimaryKey()
	{
		var primaryKey = Context.Schema.Columns.FirstOrDefault(f => f.IsPrimaryKey);

		if (primaryKey is not null)
			await new PrimaryKeyAdd(primaryKey).Execute(Context);
	}

	private async Task ExecuteDefaults()
	{
		var name = Temporary ? TemporaryName : Context.Schema.Name;

		foreach (var column in Context.Schema.Columns)
		{
			if (!string.IsNullOrWhiteSpace(column.DefaultValue))
				await new DefaultAdd(column, name).Execute(Context);
		}
	}

	private async Task ExecuteIndexes()
	{
		var indexes = ParseIndexes(Context.Schema);

		foreach (var index in indexes)
			await new IndexCreate(index).Execute(Context);
	}

	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			var name = Temporary ? TemporaryName : Context.Schema.Name;

			text.AppendLine($"CREATE TABLE {Escape(Context.Schema.SchemaName(), name)}");
			text.AppendLine("(");
			var comma = string.Empty;

			for (var i = 0; i < Context.Schema.Columns.Count; i++)
			{
				text.AppendLine($"{comma} {CreateColumnCommandText(Context.Schema.Columns[i])}");

				comma = ",";
			}

			text.AppendLine(");");

			return text.ToString();
		}
	}
}
