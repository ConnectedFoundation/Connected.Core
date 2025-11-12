using Connected.Annotations.Entities;
using Connected.Storage.Schemas;
using System.Collections.Immutable;
using System.Data;
using System.Reflection;

namespace Connected.Storage.Sql.Schemas;

internal class ExistingColumn(ISchema schema)
	: ISchemaColumn, IExistingSchemaColumn
{
	public required string Name { get; set; }

	public DbType DataType { get; set; }

	public bool IsIdentity { get; set; }
	public bool IsVersion { get; set; }

	public bool IsUnique { get; set; }

	public bool IsIndex { get; set; }

	public bool IsPrimaryKey { get; set; }

	public string? DefaultValue { get; set; }

	public int MaxLength { get; set; }

	public bool IsNullable { get; set; }

	public string? DependencyType { get; set; }

	public string? DependencyProperty { get; set; }

	public string? Index { get; set; }
	public int Precision { get; set; }
	public int Scale { get; set; }

	public DateKind DateKind { get; set; } = DateKind.DateTime;
	public BinaryKind BinaryKind { get; set; } = BinaryKind.VarBinary;

	public PropertyInfo? Property { get; set; }
	public int DatePrecision { get; set; }

	public ImmutableArray<string> QueryIndexColumns(string column)
	{
		if (schema is not ExistingSchema existing)
			return [];

		foreach (var index in existing.Indexes)
		{
			if (index.Columns.Contains(column, StringComparer.OrdinalIgnoreCase))
				return [.. index.Columns];
		}

		return [];
	}
}
