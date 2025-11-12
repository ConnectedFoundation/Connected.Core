using Connected.Storage.Schemas;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class ColumnAlter(ISchemaColumn column, ExistingSchema existing, ISchemaColumn existingColumn) : ColumnTransaction(column)
{
	protected override async Task OnExecute()
	{
		if (ColumnComparer.Default.Equals(existingColumn, Column))
			return;

		if (!string.IsNullOrWhiteSpace(existingColumn.DefaultValue))
		{
			var existingDefault = SchemaExtensions.ParseDefaultValue(existingColumn.DefaultValue);
			var def = SchemaExtensions.ParseDefaultValue(Column.DefaultValue);

			if (!string.Equals(existingDefault, def, StringComparison.Ordinal))
				await new DefaultDrop(Column).Execute(Context);
		}
		if (Column.DataType != existingColumn.DataType
			|| Column.IsNullable != existingColumn.IsNullable
			|| Column.MaxLength != existingColumn.MaxLength
			|| Column.IsVersion != existingColumn.IsVersion)
			await Context.Execute(CommandText);

		var ed = SchemaExtensions.ParseDefaultValue(existingColumn.DefaultValue);
		var nd = SchemaExtensions.ParseDefaultValue(Column.DefaultValue);

		if (!string.Equals(ed, nd, StringComparison.Ordinal) && nd is not null)
			await new DefaultAdd(Column, Context.Schema.Name).Execute(Context);

		if (!existingColumn.IsPrimaryKey && Column.IsPrimaryKey)
			await new PrimaryKeyAdd(Column).Execute(Context);
		else if (existingColumn.IsPrimaryKey && !Column.IsPrimaryKey)
			await new PrimaryKeyRemove(existing, existingColumn).Execute(Context);
	}

	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.Schema, Context.Schema.Name)}");
			text.AppendLine($"ALTER COLUMN {CreateColumnCommandText(Column)}");

			return text.ToString();
		}
	}
}
