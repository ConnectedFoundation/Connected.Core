using Connected.Annotations.Entities;
using Connected.Reflection;
using Connected.Storage.Schemas;
using System.Data;

namespace Connected.Storage.Sql.Schemas;

internal static class SchemaExtensions
{
	public const string FileGroup = "PRIMARY";

	public static ITable? Find(this List<ITable> tables, string schema, string name)
	{
		return tables.FirstOrDefault(f => string.Equals(schema, f.Schema, StringComparison.OrdinalIgnoreCase) && string.Equals(name, f.Name, StringComparison.OrdinalIgnoreCase));
	}

	public static ITableColumn? FindColumn(this ITable table, string name)
	{
		return table.TableColumns.FirstOrDefault(f => string.Equals(name, f.Name, StringComparison.OrdinalIgnoreCase));
	}

	public static ITable? FindPrimaryKeyTable(this IDatabase database, string name)
	{
		foreach (var i in database.Tables)
		{
			foreach (var j in i.TableColumns)
			{
				foreach (var k in j.Constraints)
				{
					if (string.Equals(k.Type, "PRIMARY KEY", StringComparison.OrdinalIgnoreCase) && string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase))
						return i;
				}
			}
		}

		return null;
	}

	public static ITableColumn? FindPrimaryKeyColumn(this IDatabase database, string name)
	{
		foreach (var i in database.Tables)
		{
			foreach (var j in i.TableColumns)
			{
				foreach (var k in j.Constraints)
				{
					if (string.Equals(k.Type, "PRIMARY KEY", StringComparison.OrdinalIgnoreCase) && string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase))
						return j;
				}
			}
		}

		return null;
	}

	public static ITableColumn? ResolvePrimaryKeyColumn(this ITable table)
	{
		foreach (var i in table.TableColumns)
		{
			foreach (var j in i.Constraints)
			{
				if (string.Equals(j.Type, "PRIMARY KEY", StringComparison.OrdinalIgnoreCase))
					return i;
			}
		}

		return null;
	}

	public static ITableConstraint? ResolvePrimaryKey(this ITable table)
	{
		foreach (var i in table.TableColumns)
		{
			foreach (var j in i.Constraints)
			{
				if (string.Equals(j.Type, "PRIMARY KEY", StringComparison.OrdinalIgnoreCase))
					return j;
			}
		}

		return null;
	}

	public static List<ITableColumn> ResolveDefaults(this ITable table)
	{
		var r = new List<ITableColumn>();

		foreach (var i in table.TableColumns)
		{
			if (!string.IsNullOrWhiteSpace(i.DefaultValue))
				r.Add(i);
		}

		return r;
	}

	public static List<ITableConstraint> ResolveUniqueConstraints(this ITable table)
	{
		var r = new List<ITableConstraint>();

		foreach (var i in table.TableColumns)
		{
			foreach (var j in i.Constraints)
			{
				if (string.Equals(j.Type, "UNIQUE", StringComparison.OrdinalIgnoreCase) && r.FirstOrDefault(f => string.Equals(f.Name, j.Name, StringComparison.OrdinalIgnoreCase)) is null)
					r.Add(j);
			}
		}
		return r;
	}

	public static ITableColumn? FindUniqueConstraintColumn(this IDatabase database, string name)
	{
		foreach (var table in database.Tables)
		{
			foreach (var column in table.TableColumns)
			{
				foreach (var constraint in column.Constraints)
				{
					if (string.Equals(constraint.Type, "UNIQUE", StringComparison.OrdinalIgnoreCase) && string.Equals(constraint.Name, name, StringComparison.OrdinalIgnoreCase))
						return column;
				}
			}
		}

		return null;
	}

	public static T GetValue<T>(this IDataReader r, string fieldName, T defaultValue)
	{
		var idx = r.GetOrdinal(fieldName);

		if (idx == -1)
			return defaultValue;

		if (r.IsDBNull(idx))
			return defaultValue;

		if (Types.Convert<T>(r.GetValue(idx)) is T result)
			return result;

		return defaultValue;
	}

	public static string SchemaName(this ISchema schema)
	{
		return string.IsNullOrWhiteSpace(schema.Schema) ? SchemaAttribute.DefaultSchema : schema.Schema;
	}

	public static string ParseDefaultValue(string value)
	{
		if (string.IsNullOrEmpty(value))
			return value;

		if (value.StartsWith("N'"))
			return value;

		var defValue = $"N'{value}'";

		if (value.Length > 1)
		{
			var last = value.Trim()[^1];
			var prev = value.Trim()[0..^1].Trim()[^1];

			if (last == ')' && prev == '(')
				defValue = value;
		}

		return defValue;
	}

	public static DbType ToDbType(string? value)
	{
		if (string.Equals(value, "bigint", StringComparison.OrdinalIgnoreCase))
			return DbType.Int64;
		else if (string.Equals(value, "binary", StringComparison.OrdinalIgnoreCase))
			return DbType.Binary;
		else if (string.Equals(value, "bit", StringComparison.OrdinalIgnoreCase))
			return DbType.Boolean;
		else if (string.Equals(value, "char", StringComparison.OrdinalIgnoreCase))
			return DbType.AnsiStringFixedLength;
		else if (string.Equals(value, "date", StringComparison.OrdinalIgnoreCase))
			return DbType.Date;
		else if (string.Equals(value, "datetime", StringComparison.OrdinalIgnoreCase))
			return DbType.DateTime;
		else if (string.Equals(value, "datetime2", StringComparison.OrdinalIgnoreCase))
			return DbType.DateTime2;
		else if (string.Equals(value, "datetimeoffset", StringComparison.OrdinalIgnoreCase))
			return DbType.DateTimeOffset;
		else if (string.Equals(value, "decimal", StringComparison.OrdinalIgnoreCase))
			return DbType.Decimal;
		else if (string.Equals(value, "float", StringComparison.OrdinalIgnoreCase))
			return DbType.Double;
		else if (string.Equals(value, "geography", StringComparison.OrdinalIgnoreCase))
			return DbType.Object;
		else if (string.Equals(value, "hierarchyid", StringComparison.OrdinalIgnoreCase))
			return DbType.Object;
		else if (string.Equals(value, "image", StringComparison.OrdinalIgnoreCase))
			return DbType.Binary;
		else if (string.Equals(value, "int", StringComparison.OrdinalIgnoreCase))
			return DbType.Int32;
		else if (string.Equals(value, "money", StringComparison.OrdinalIgnoreCase))
			return DbType.Currency;
		else if (string.Equals(value, "nchar", StringComparison.OrdinalIgnoreCase))
			return DbType.StringFixedLength;
		else if (string.Equals(value, "ntext", StringComparison.OrdinalIgnoreCase))
			return DbType.String;
		else if (string.Equals(value, "numeric", StringComparison.OrdinalIgnoreCase))
			return DbType.VarNumeric;
		else if (string.Equals(value, "nvarchar", StringComparison.OrdinalIgnoreCase))
			return DbType.String;
		else if (string.Equals(value, "real", StringComparison.OrdinalIgnoreCase))
			return DbType.Single;
		else if (string.Equals(value, "smalldatetime", StringComparison.OrdinalIgnoreCase))
			return DbType.DateTime;
		else if (string.Equals(value, "smallmoney", StringComparison.OrdinalIgnoreCase))
			return DbType.Currency;
		else if (string.Equals(value, "sql_variant", StringComparison.OrdinalIgnoreCase))
			return DbType.Object;
		else if (string.Equals(value, "text", StringComparison.OrdinalIgnoreCase))
			return DbType.String;
		else if (string.Equals(value, "time", StringComparison.OrdinalIgnoreCase))
			return DbType.Time;
		else if (string.Equals(value, "timestamp", StringComparison.OrdinalIgnoreCase))
			return DbType.Binary;
		else if (string.Equals(value, "tinyint", StringComparison.OrdinalIgnoreCase))
			return DbType.Byte;
		else if (string.Equals(value, "uniqueidentifier", StringComparison.OrdinalIgnoreCase))
			return DbType.Guid;
		else if (string.Equals(value, "varbinary", StringComparison.OrdinalIgnoreCase))
			return DbType.Binary;
		else if (string.Equals(value, "varchar", StringComparison.OrdinalIgnoreCase))
			return DbType.AnsiString;
		else if (string.Equals(value, "xml", StringComparison.OrdinalIgnoreCase))
			return DbType.Xml;
		else
			return DbType.String;
	}
}