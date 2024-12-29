using Connected.Storage.Schemas;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class ColumnDrop(ISchemaColumn column, ExistingSchema existing) : ColumnTransaction(column)
{
	protected override async Task OnExecute()
	{
		if (!string.IsNullOrWhiteSpace(Column.DefaultValue))
			await new DefaultDrop(Column).Execute(Context);

		var indexes = existing.ResolveIndexes(Column.Name);

		foreach (var index in indexes)
			await new IndexDrop(index).Execute(Context);

		await Context.Execute(CommandText);
	}

	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.SchemaName(), Context.Schema.Name)} DROP COLUMN {Escape(Column.Name)};");

			return text.ToString();
		}
	}
}