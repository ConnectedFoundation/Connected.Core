using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class UniqueConstraintAdd(IndexDescriptor index)
		: TableTransaction
{
	private IndexDescriptor Index { get; } = index;

	protected override async Task OnExecute()
	{
		await Context.Execute(CommandText);
	}

	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.SchemaName(), Context.Schema.Name)}");
			text.AppendLine($"ADD CONSTRAINT [{Context.GenerateConstraintName(Context.Schema.SchemaName(), Context.Schema.Name, ConstraintNameType.Index)}] UNIQUE NONCLUSTERED (");
			var comma = string.Empty;

			foreach (var column in Index.Columns)
			{
				text.AppendLine($"{comma}{Escape(column)} ASC");

				comma = ",";
			}

			text.AppendLine($") ON {Escape(SchemaExtensions.FileGroup)}");

			return text.ToString();
		}
	}
}
