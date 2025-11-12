using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class TableExists
	: SynchronizationQuery<bool>
{
	protected override async Task<bool> OnExecute()
	{
		var result = await Context.Select(CommandText);

		if (result is null)
			return false;

		return result.Result;
	}

	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"IF (EXISTS (SELECT *  FROM INFORMATION_SCHEMA.TABLES  WHERE TABLE_SCHEMA = '{Unescape(Context.Schema.Schema)}' AND TABLE_NAME = '{Unescape(Context.Schema.Name)}')) SELECT 1 as result ELSE SELECT 0 as result");

			return text.ToString();
		}
	}
}
