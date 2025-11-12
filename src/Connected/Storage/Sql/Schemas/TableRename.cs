using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class TableRename(string temporaryName)
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

			text.AppendLine($"EXECUTE sp_rename N'{Unescape(Context.Schema.Schema)}.{Unescape(temporaryName)}', N'{Unescape(Context.Schema.Name)}', 'OBJECT'");

			return text.ToString();
		}
	}
}
