using Connected.Annotations.Entities;
using Connected.Storage.Schemas;
using System.Data;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

internal abstract class TableTransaction : SynchronizationTransaction
{
	protected static string CreateColumnCommandText(ISchemaColumn column)
	{
		var builder = new StringBuilder();

		builder.AppendFormat($"{Escape(column.Name)} {CreateDataTypeMetaData(column)} ");

		if (column.IsIdentity)
			builder.Append("IDENTITY(1,1) ");

		if (column.IsNullable)
			builder.Append("NULL ");
		else
			builder.Append("NOT NULL ");

		return builder.ToString();
	}

	protected static string ResolveColumnLength(ISchemaColumn column)
	{
		if (column.MaxLength == -1)
			return "MAX";

		if (column.MaxLength > 0)
			return column.MaxLength.ToString();

		return column.DataType switch
		{
			DbType.AnsiString or DbType.String or DbType.AnsiStringFixedLength or DbType.StringFixedLength => 50.ToString(),
			DbType.Binary => 128.ToString(),
			DbType.Time or DbType.DateTime2 or DbType.DateTimeOffset => column.DatePrecision.ToString(),
			DbType.VarNumeric => 8.ToString(),
			DbType.Xml => "MAX",
			DbType.Decimal => $"{column.Precision}, {column.Scale}",
			_ => 50.ToString(),
		};
	}

	protected static string CreateDataTypeMetaData(ISchemaColumn column)
	{
		return column.DataType switch
		{
			DbType.AnsiString => $"[varchar]({ResolveColumnLength(column)})",
			DbType.Binary => column.IsVersion ? "[timestamp]" : column.BinaryKind == BinaryKind.Binary ? $"[binary]({ResolveColumnLength(column)})" : $"[varbinary]({ResolveColumnLength(column)})",
			DbType.Byte => "[tinyint]",
			DbType.Boolean => "[bit]",
			DbType.Currency => "[money]",
			DbType.Date => "[date]",
			DbType.DateTime => column.DateKind == DateKind.SmallDateTime ? "[smalldatetime]" : "[datetime]",
			DbType.Decimal => $"[decimal]({ResolveColumnLength(column)})",
			DbType.Double => "[float]",
			DbType.Guid => "[uniqueidentifier]",
			DbType.Int16 => "[smallint]",
			DbType.Int32 => "[int]",
			DbType.Int64 => "[bigint]",
			DbType.Object => $"[varbinary]({ResolveColumnLength(column)})",
			DbType.SByte => "[smallint]",
			DbType.Single => "[real]",
			DbType.String => $"[nvarchar]({ResolveColumnLength(column)})",
			DbType.Time => $"[time]({ResolveColumnLength(column)})",
			DbType.UInt16 => "[int]",
			DbType.UInt32 => "[bigint]",
			DbType.UInt64 => "[float]",
			DbType.VarNumeric => $"[numeric]({ResolveColumnLength(column)})",
			DbType.AnsiStringFixedLength => $"[char]({ResolveColumnLength(column)})",
			DbType.StringFixedLength => $"[nchar]({ResolveColumnLength(column)})",
			DbType.Xml => "[xml]",
			DbType.DateTime2 => $"[datetime2]({ResolveColumnLength(column)})",
			DbType.DateTimeOffset => $"[datetimeoffset]({ResolveColumnLength(column)})",
			_ => throw new NotSupportedException(),
		};
	}

	protected static List<IndexDescriptor> ParseIndexes(ISchema schema)
	{
		var result = new List<IndexDescriptor>();

		foreach (var column in schema.Columns)
		{
			if (column.IsPrimaryKey)
				continue;

			if (column.IsIndex)
			{
				if (string.IsNullOrWhiteSpace(column.Index))
				{
					var index = new IndexDescriptor
					{
						Unique = column.IsUnique,
					};

					index.Columns.Add(column.Name);

					result.Add(index);
				}
				else
				{
					var index = result.FirstOrDefault(f => string.Equals(f.Group, column.Index, StringComparison.OrdinalIgnoreCase));

					if (index is null)
					{
						index = new IndexDescriptor
						{
							Group = column.Index,
							Unique = column.IsUnique
						};

						result.Add(index);
					}

					index.Columns.Add(column.Name);
				}
			}
		}

		return result;
	}
}
