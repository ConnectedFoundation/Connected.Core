using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class TableExists : SynchronizationQuery<bool>
{
	protected override async Task<bool> OnExecute()
	{
		return (await Context.Select(CommandText)).Result;
	}

	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"IF (EXISTS (SELECT *  FROM INFORMATION_SCHEMA.TABLES  WHERE TABLE_SCHEMA = '{Unescape(Context.Schema.SchemaName())}' AND TABLE_NAME = '{Unescape(Context.Schema.Name)}')) SELECT 1 as result ELSE SELECT 0 as result");

			return text.ToString();
		}
	}
}
