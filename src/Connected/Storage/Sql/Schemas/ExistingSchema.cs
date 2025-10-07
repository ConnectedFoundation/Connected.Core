using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

internal class ExistingSchema : ISchema
{
	public ExistingSchema()
	{
		Columns = [];
	}

	public List<ISchemaColumn> Columns { get; }

	public string? Schema { get; set; }

	public string? Name { get; set; }

	public string? Type { get; set; }

	public bool Ignore { get; set; }

	public ObjectDescriptor? Descriptor { get; private set; }

	public async Task Load(SchemaExecutionContext context)
	{
		Name = context.Schema.Name;
		Type = context.Schema.Type;
		Schema = context.Schema.SchemaName();

		Columns.AddRange(await new Columns(this).Execute(context));
		Descriptor = await new SpHelp().Execute(context);

		if (Columns.FirstOrDefault(f => string.Equals(f.Name, Descriptor.Identity.Identity, StringComparison.OrdinalIgnoreCase)) is ExistingColumn c)
			c.IsIdentity = true;

		foreach (var index in Descriptor.Indexes)
		{
			foreach (var column in index.Columns)
			{
				if (Columns.FirstOrDefault(f => string.Equals(column, f.Name, StringComparison.OrdinalIgnoreCase)) is not ExistingColumn col)
					continue;

				switch (index.Type)
				{
					case IndexType.Index:
						col.IsIndex = true;
						break;
					case IndexType.Unique:
						col.IsIndex = true;
						col.IsUnique = true;
						break;
					case IndexType.PrimaryKey:
						col.IsPrimaryKey = true;
						col.IsIndex = true;
						col.IsUnique = true;
						break;
				}
			}
		}

		foreach (var constraint in Descriptor.Constraints)
		{
			switch (constraint.ConstraintType)
			{
				case ConstraintType.Default:
					if (Columns.FirstOrDefault(f => string.Equals(f.Name, constraint.Columns[0], StringComparison.OrdinalIgnoreCase)) is ExistingColumn column)
						column.DefaultValue = constraint.DefaultValue;
					break;
			}
		}
	}

	public List<ObjectIndex> Indexes
	{
		get
		{
			var result = new List<ObjectIndex>();

			foreach (var column in Columns)
			{
				var indexes = ResolveIndexes(column.Name);

				foreach (var index in indexes)
				{
					if (result.FirstOrDefault(f => string.Equals(f.Name, index.Name, StringComparison.OrdinalIgnoreCase)) is null)
						result.Add(index);
				}
			}

			return result;
		}
	}
	public List<ObjectIndex> ResolveIndexes(string column)
	{
		var result = new List<ObjectIndex>();

		foreach (var index in Descriptor.Indexes)
		{
			if (index.IsReferencedBy(column))
				result.Add(index);
		}

		return result;
	}

	public bool Equals(ISchema? other)
	{
		throw new NotImplementedException();
	}
}
