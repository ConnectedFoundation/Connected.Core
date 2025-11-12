using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class IndexCreate(IndexDescriptor index) : TableTransaction
{
	protected override async Task OnExecute()
	{
		if (index.Unique)
			await new UniqueConstraintAdd(index).Execute(Context);
		else
			await Context.Execute(CommandText);
	}

	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"CREATE NONCLUSTERED INDEX [{Context.GenerateConstraintName(Context.Schema.Schema, Context.Schema.Name, ConstraintNameType.Index)}] ON {Escape(Context.Schema.Schema, Context.Schema.Name)}(");
			var comma = string.Empty;

			foreach (var column in index.Columns)
			{
				text.AppendLine($"{comma}{Escape(column)} ASC");

				comma = ",";
			}

			text.AppendLine($") ON {Escape(SchemaExtensions.FileGroup)}");

			return text.ToString();
		}
	}
}
