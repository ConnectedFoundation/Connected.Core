using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

internal class TableSynchronize : TableTransaction
{
	private ExistingSchema _existingSchema = new();

	private bool TableExists { get; set; }

	protected override async Task OnExecute()
	{
		TableExists = await new TableExists().Execute(Context);

		if (!TableExists)
		{
			await new TableCreate(false).Execute(Context);
			return;
		}

		await _existingSchema.Load(Context);

		Context.ExistingSchema = _existingSchema;

		if (ShouldRecreate)
			await new TableRecreate(_existingSchema).Execute(Context);
		else if (ShouldAlter)
			await new TableAlter(_existingSchema).Execute(Context);
	}

	private bool ShouldAlter => !Context.Schema.Equals(ExistingSchema);
	private bool ShouldRecreate => HasIdentityChanged || HasColumnMetadataChanged;

	private ExistingSchema ExistingSchema => _existingSchema;

	private bool HasIdentityChanged
	{
		get
		{
			foreach (var column in Context.Schema.Columns)
			{
				if (ExistingSchema.Columns.FirstOrDefault(f => string.Equals(f.Name, column.Name, StringComparison.OrdinalIgnoreCase)) is not ISchemaColumn existing)
					return true;

				if (existing.IsIdentity != column.IsIdentity)
					return true;
			}

			foreach (var existing in ExistingSchema.Columns)
			{
				var column = Context.Schema.Columns.FirstOrDefault(f => string.Equals(f.Name, existing.Name, StringComparison.OrdinalIgnoreCase));

				if (column is null && existing.IsIdentity)
					return true;
				else if (column is not null && column.IsIdentity != existing.IsIdentity)
					return true;
			}

			return false;
		}
	}

	private bool HasColumnMetadataChanged
	{
		get
		{
			foreach (var existing in ExistingSchema.Columns)
			{
				if (Context.Schema.Columns.FirstOrDefault(f => string.Equals(f.Name, existing.Name, StringComparison.OrdinalIgnoreCase)) is not ISchemaColumn column)
					continue;

				if (column.DataType != existing.DataType
					|| column.MaxLength != existing.MaxLength
					|| column.IsNullable != existing.IsNullable
					|| column.IsVersion != existing.IsVersion
					|| column.Precision != existing.Precision
					|| column.Scale != existing.Scale
					|| column.DateKind != existing.DateKind
					|| column.BinaryKind != existing.BinaryKind
					|| column.DatePrecision != existing.DatePrecision)
					return true;
			}

			return false;
		}
	}
}
