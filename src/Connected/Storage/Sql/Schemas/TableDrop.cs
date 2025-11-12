using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class TableDrop
	: TableTransaction
{
	protected override async Task OnExecute()
	{
		await Context.Execute(CommandText);
	}

	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"DROP TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)}");

			return text.ToString();
		}
	}
}
