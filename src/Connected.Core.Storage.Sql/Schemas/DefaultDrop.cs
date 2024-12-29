using Connected.Storage.Schemas;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal class DefaultDrop(ISchemaColumn column) : ColumnTransaction(column)
{
	protected override async Task OnExecute()
	{
		if (string.IsNullOrWhiteSpace(DefaultName))
			return;

		await Context.Execute(CommandText);
	}

	private string CommandText
	{
		get
		{
			var text = new StringBuilder();

			text.AppendLine($"ALTER TABLE {Escape(Context.Schema.SchemaName(), Context.Schema.Name)}");
			text.AppendLine($"DROP CONSTRAINT {DefaultName};");

			return text.ToString();
		}
	}

	private string? DefaultName
	{
		get
		{
			if (Context.ExistingSchema is null)
				return null;

			foreach (var constraint in Context.ExistingSchema.Descriptor.Constraints)
			{
				if (constraint.ConstraintType == ConstraintType.Default)
				{
					if (constraint.Columns.Count == 1 && string.Equals(constraint.Columns[0], Column.Name, StringComparison.OrdinalIgnoreCase))
						return constraint.Name;
				}
			}

			return null;
		}
	}
}
