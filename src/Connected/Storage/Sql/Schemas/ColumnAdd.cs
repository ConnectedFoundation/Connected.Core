using Connected.Storage.Schemas;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class ColumnAdd(ISchemaColumn column)
	: ColumnTransaction(column)
{
	protected override async Task OnExecute()
	{
		await Context.Execute(CommandText);

		if (Column.IsPrimaryKey)
			await new PrimaryKeyAdd(Column).Execute(Context);

		if (!string.IsNullOrWhiteSpace(Column.DefaultValue))
			await new DefaultAdd(Column, Context.Schema.Name).Execute(Context);
	}

	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.SchemaName(), Context.Schema.Name)}");
			text.AppendLine($"ADD COLUMN {CreateColumnCommandText(Column)}");

			return text.ToString();
		}
	}
}
