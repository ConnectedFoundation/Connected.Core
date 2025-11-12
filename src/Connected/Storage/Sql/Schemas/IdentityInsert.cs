using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class IdentityInsert(string tableName, bool on) : TableTransaction
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
			var switchCommand = on ? "ON" : "OFF";

			text.AppendLine($"SET IDENTITY_INSERT {Escape(Context.Schema.Schema, tableName)}  {switchCommand}");

			return text.ToString();
		}
	}
}
