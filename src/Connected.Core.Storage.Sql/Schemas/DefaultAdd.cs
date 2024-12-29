using Connected.Storage.Schemas;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class DefaultAdd(ISchemaColumn column, string tableName) : ColumnTransaction(column)
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

			var defValue = SchemaExtensions.ParseDefaultValue(Column.DefaultValue);

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.SchemaName(), tableName)}");
			text.AppendLine($"ADD CONSTRAINT {Context.GenerateConstraintName(Context.Schema.SchemaName(), tableName, ConstraintNameType.Default)} DEFAULT {defValue} FOR {Column.Name}");

			return text.ToString();
		}
	}
}
