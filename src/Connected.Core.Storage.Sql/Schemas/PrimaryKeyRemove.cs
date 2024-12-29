using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

internal class PrimaryKeyRemove(ExistingSchema existing, ISchemaColumn column) : ColumnTransaction(column)
{
	protected override async Task OnExecute()
	{
		if (existing.Indexes.FirstOrDefault(f => f.Type == IndexType.PrimaryKey) is ObjectIndex constraint)
		{
			await new ConstraintDrop(constraint).Execute(Context);

			existing.Indexes.Remove(constraint);
		}
	}
}
