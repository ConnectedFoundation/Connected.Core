using Connected.Reflection;
using Connected.Storage.Schemas;
using System.Data;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Provides extension methods and utilities for schema operations.
/// </summary>
/// <remarks>
/// This static class contains helper methods for working with schema metadata including table
/// and column lookups, constraint resolution, primary key discovery, and data type conversions.
/// It provides methods for navigating schema structures, extracting metadata, and converting
/// between SQL Server data types and DbType enumeration values. The class also includes utilities
/// for default value parsing and data reader value extraction with type safety. These methods
/// are used throughout the schema synchronization system to query and manipulate schema metadata
/// efficiently.
/// </remarks>
internal static class SchemaExtensions
{
	/// <summary>
	/// The default filegroup name for SQL Server database objects.
	/// </summary>
	public const string FileGroup = "PRIMARY";

	/// <summary>
	/// Finds a table in a collection by schema and name.
	/// </summary>
	/// <param name="tables">The collection of tables to search.</param>
	/// <param name="schema">The schema name.</param>
	/// <param name="name">The table name.</param>
	/// <returns>The matching table, or <c>null</c> if not found.</returns>
	public static ITable? Find(this List<ITable> tables, string schema, string name)
	{
		return tables.FirstOrDefault(f => string.Equals(schema, f.Schema, StringComparison.OrdinalIgnoreCase) && string.Equals(name, f.Name, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Finds a column in a table by name.
	/// </summary>
	/// <param name="table">The table to search.</param>
	/// <param name="name">The column name.</param>
	/// <returns>The matching column, or <c>null</c> if not found.</returns>
	public static ITableColumn? FindColumn(this ITable table, string name)
	{
		return table.TableColumns.FirstOrDefault(f => string.Equals(name, f.Name, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Finds the table containing a primary key constraint with the specified name.
	/// </summary>
	/// <param name="database">The database to search.</param>
	/// <param name="name">The primary key constraint name.</param>
	/// <returns>The table containing the primary key, or <c>null</c> if not found.</returns>
	public static ITable? FindPrimaryKeyTable(this IDatabase database, string name)
	{
		/*
		 * Search through all tables and their columns to find the primary key constraint.
		 */
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

	/// <summary>
	/// Finds the column with a primary key constraint with the specified name.
	/// </summary>
	/// <param name="database">The database to search.</param>
	/// <param name="name">The primary key constraint name.</param>
	/// <returns>The column with the primary key, or <c>null</c> if not found.</returns>
	public static ITableColumn? FindPrimaryKeyColumn(this IDatabase database, string name)
	{
		/*
		 * Search through all tables and their columns to find the primary key column.
		 */
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

	/// <summary>
	/// Resolves the primary key column for a table.
	/// </summary>
	/// <param name="table">The table to search.</param>
	/// <returns>The primary key column, or <c>null</c> if the table has no primary key.</returns>
	public static ITableColumn? ResolvePrimaryKeyColumn(this ITable table)
	{
		/*
		 * Find the first column with a PRIMARY KEY constraint.
		 */
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

	/// <summary>
	/// Resolves the primary key constraint for a table.
	/// </summary>
	/// <param name="table">The table to search.</param>
	/// <returns>The primary key constraint, or <c>null</c> if the table has no primary key.</returns>
	public static ITableConstraint? ResolvePrimaryKey(this ITable table)
	{
		/*
		 * Find the first PRIMARY KEY constraint in the table.
		 */
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

	/// <summary>
	/// Resolves all columns with default values in a table.
	/// </summary>
	/// <param name="table">The table to search.</param>
	/// <returns>A list of columns that have default values defined.</returns>
	public static List<ITableColumn> ResolveDefaults(this ITable table)
	{
		var r = new List<ITableColumn>();

		/*
		 * Collect all columns that have non-empty default values.
		 */
		foreach (var i in table.TableColumns)
		{
			if (!string.IsNullOrWhiteSpace(i.DefaultValue))
				r.Add(i);
		}

		return r;
	}

	/// <summary>
	/// Resolves all unique constraints in a table.
	/// </summary>
	/// <param name="table">The table to search.</param>
	/// <returns>A list of unique constraints, excluding duplicates.</returns>
	public static List<ITableConstraint> ResolveUniqueConstraints(this ITable table)
	{
		var r = new List<ITableConstraint>();

		/*
		 * Collect all UNIQUE constraints, avoiding duplicates by checking constraint names.
		 */
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

	/// <summary>
	/// Finds the column with a unique constraint with the specified name.
	/// </summary>
	/// <param name="database">The database to search.</param>
	/// <param name="name">The unique constraint name.</param>
	/// <returns>The column with the unique constraint, or <c>null</c> if not found.</returns>
	public static ITableColumn? FindUniqueConstraintColumn(this IDatabase database, string name)
	{
		/*
		 * Search through all tables and their columns to find the unique constraint column.
		 */
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

	/// <summary>
	/// Gets a typed value from a data reader with a default fallback.
	/// </summary>
	/// <typeparam name="T">The type to convert the value to.</typeparam>
	/// <param name="r">The data reader.</param>
	/// <param name="fieldName">The field name to retrieve.</param>
	/// <param name="defaultValue">The default value to return if the field is null or doesn't exist.</param>
	/// <returns>The typed value from the field, or the default value.</returns>
	/// <remarks>
	/// This method safely retrieves values from a data reader, handling missing fields and null
	/// values gracefully by returning the specified default value.
	/// </remarks>
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

	/// <summary>
	/// Parses and formats a default value expression for SQL Server.
	/// </summary>
	/// <param name="value">The default value to parse.</param>
	/// <returns>The formatted default value expression suitable for SQL DDL statements.</returns>
	/// <remarks>
	/// This method ensures default values are properly formatted for use in SQL statements,
	/// adding Unicode string prefixes (N') where appropriate and preserving function calls
	/// or expressions that are already properly formatted.
	/// </remarks>
	public static string? ParseDefaultValue(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return value;

		/*
		 * Values already prefixed with N' are Unicode strings and don't need modification.
		 */
		if (value.StartsWith("N'"))
			return value;

		var defValue = $"N'{value}'";

		/*
		 * Check if the value is a function call or expression wrapped in parentheses.
		 * If so, preserve the original format.
		 */
		if (value.Length > 1)
		{
			var last = value.Trim()[^1];
			var prev = value.Trim()[0..^1].Trim()[^1];

			if (last == ')' && prev == '(')
				defValue = value;
		}

		return defValue;
	}

	/// <summary>
	/// Converts a SQL Server data type name to a DbType enumeration value.
	/// </summary>
	/// <param name="value">The SQL Server data type name.</param>
	/// <returns>The corresponding <see cref="DbType"/> value.</returns>
	/// <remarks>
	/// This method performs comprehensive mapping from SQL Server native type names to the
	/// DbType enumeration, handling all common SQL Server data types including special types
	/// like geography, hierarchyid, and sql_variant.
	/// </remarks>
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