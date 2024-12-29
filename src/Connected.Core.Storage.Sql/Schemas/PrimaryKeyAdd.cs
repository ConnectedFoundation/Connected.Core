using Connected.Storage.Schemas;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class PrimaryKeyAdd(ISchemaColumn column) : ColumnTransaction(column)
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

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.SchemaName(), Context.Schema.Name)}");
			text.AppendLine($"ADD CONSTRAINT {Context.GenerateConstraintName(Context.Schema.SchemaName(), Context.Schema.Name, ConstraintNameType.PrimaryKey)}");
			text.AppendLine($"PRIMARY KEY CLUSTERED ({Escape(Column.Name)}) ON {Escape(SchemaExtensions.FileGroup)}");

			return text.ToString();
		}
	}
}
