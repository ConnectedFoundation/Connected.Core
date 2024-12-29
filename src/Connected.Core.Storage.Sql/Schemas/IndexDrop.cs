using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class IndexDrop(ObjectIndex index) : TableTransaction
{
	protected override async Task OnExecute()
	{
		switch (index.Type)
		{
			case IndexType.Index:
				await Context.Execute(CommandText);
				break;
			case IndexType.Unique:
			case IndexType.PrimaryKey:
				await new ConstraintDrop(index).Execute(Context);
				break;
		}
	}

	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"DROP INDEX {index.Name} ON {Escape(Context.Schema.SchemaName(), Context.Schema.Name)};");

			return text.ToString();
		}
	}
}