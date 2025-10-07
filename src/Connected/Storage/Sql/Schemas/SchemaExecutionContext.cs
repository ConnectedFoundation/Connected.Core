using Connected.Storage.Schemas;
using System.Data;

namespace Connected.Storage.Sql.Schemas;

internal class SchemaExecutionContext(IStorageProvider storage, ISchema schema, string connectionString)
{
	private ExistingSchema _existingSchema;

	public Version Version { get; } = SqlDataConnection.ResolveVersion(connectionString);

	public ExistingSchema ExistingSchema
	{
		get => _existingSchema; set
		{
			_existingSchema = value;

			if (_existingSchema is null)
				return;

			foreach (var index in _existingSchema.Descriptor.Constraints)
			{
				switch (index.ConstraintType)
				{
					case ConstraintType.Default:
						AddConstraint(ConstraintNameType.Default, index.Name);
						break;
					case ConstraintType.PrimaryKey:
						AddConstraint(ConstraintNameType.PrimaryKey, index.Name);
						break;
					case ConstraintType.Unique:
						AddConstraint(ConstraintNameType.Index, index.Name);
						break;
				}
			}

		}
	}

	public IStorageProvider Storage { get; } = storage;
	public ISchema Schema { get; } = schema;
	public Dictionary<ConstraintNameType, List<string>> Constraints { get; } = [];

	public async Task Execute(string commandText)
	{
		await Storage.Open<AdHocSchemaEntity>().Execute(new SchemaStorageDto
		{
			Operation = new SqlStorageOperation
			{
				CommandText = commandText
			},
			ConnectionType = typeof(SqlDataConnection),
			ConnectionString = connectionString
		});
	}

	public async Task<AdHocSchemaEntity?> Select(string commandText)
	{
		return await Storage.Open<AdHocSchemaEntity>().Select(new SchemaStorageDto
		{
			Operation = new SqlStorageOperation
			{
				CommandText = commandText
			},
			ConnectionType = typeof(SqlDataConnection),
			ConnectionString = connectionString
		});
	}

	public async Task<IDataReader?> OpenReader(IStorageOperation operation)
	{
		var readers = await Storage.Open<AdHocSchemaEntity>().OpenReaders(new SchemaStorageDto
		{
			Operation = operation,
			ConnectionType = typeof(SqlDataConnection),
			ConnectionString = connectionString
		});

		return readers[0];
	}

	private void AddConstraint(ConstraintNameType type, string name)
	{
		if (!Constraints.TryGetValue(type, out List<string>? existing))
		{
			existing = new List<string>();

			Constraints.Add(type, existing);
		}

		if (!ConstraintNameExists(name))
			existing.Add(name);
	}

	public string GenerateConstraintName(string schema, string tableName, ConstraintNameType type)
	{
		var index = 0;

		while (true)
		{
			var value = $"{ConstraintPrefix(type)}_{schema.ToLowerInvariant()}_{tableName}";

			if (index > 0)
				value = $"{value}_{index}";

			if (!ConstraintNameExists(value))
			{
				AddConstraint(type, value);
				return value;
			}

			index++;
		}
	}

	private bool ConstraintNameExists(string value)
	{
		foreach (var key in Constraints)
		{
			foreach (var item in key.Value)
			{
				if (item.Contains(value, StringComparison.OrdinalIgnoreCase))
					return true;
			}
		}

		return false;
	}

	private static string ConstraintPrefix(ConstraintNameType type)
	{
		return type switch
		{
			ConstraintNameType.Default => "DF",
			ConstraintNameType.PrimaryKey => "PK",
			ConstraintNameType.Index => "IX",
			_ => "IX"
		};
	}
}
