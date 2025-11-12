using Connected.Annotations.Entities;
using Connected.Storage.Schemas;
using System.Data;
using System.Text;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Base class for table-related schema synchronization transactions.
/// </summary>
/// <remarks>
/// This abstract class provides common functionality for operations that work with entire tables
/// during schema synchronization. It extends <see cref="SynchronizationTransaction"/> and adds
/// utility methods for generating SQL DDL statements including column definitions with appropriate
/// data types, lengths, and constraints. The class includes sophisticated data type mapping logic
/// that converts from DbType enumeration values to SQL Server-specific type declarations with
/// proper length, precision, and scale specifications. It also provides index parsing functionality
/// to extract index definitions from schema column metadata, supporting both single-column and
/// multi-column indexes with unique constraints.
/// </remarks>
internal abstract class TableTransaction
	: SynchronizationTransaction
{
	/// <summary>
	/// Creates the SQL command text for a column definition.
	/// </summary>
	/// <param name="column">The column to generate definition for.</param>
	/// <returns>The SQL column definition string including name, data type, identity, and nullability.</returns>
	protected static string CreateColumnCommandText(ISchemaColumn column)
	{
		var builder = new StringBuilder();

		builder.AppendFormat($"{Escape(column.Name)} {CreateDataTypeMetaData(column)} ");

		/*
		 * Add IDENTITY specification if the column has identity property.
		 */
		if (column.IsIdentity)
			builder.Append("IDENTITY(1,1) ");

		/*
		 * Add nullability specification.
		 */
		if (column.IsNullable)
			builder.Append("NULL ");
		else
			builder.Append("NOT NULL ");

		return builder.ToString();
	}

	/// <summary>
	/// Resolves the appropriate length or precision value for a column type.
	/// </summary>
	/// <param name="column">The column to resolve length for.</param>
	/// <returns>The length value as a string, which may be "MAX" for maximum length types.</returns>
	/// <remarks>
	/// This method handles special cases like MAX length for large text/binary types,
	/// precision/scale for decimal types, and fractional seconds precision for temporal types.
	/// </remarks>
	protected static string ResolveColumnLength(ISchemaColumn column)
	{
		if (column.MaxLength == -1)
			return "MAX";

		if (column.MaxLength > 0)
			return column.MaxLength.ToString();

		/*
		 * Apply default lengths based on data type when not explicitly specified.
		 */
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

	/// <summary>
	/// Creates the SQL Server data type declaration for a column.
	/// </summary>
	/// <param name="column">The column to create data type for.</param>
	/// <returns>The SQL Server data type declaration with appropriate length, precision, or scale.</returns>
	/// <exception cref="NotSupportedException">Thrown when the data type is not supported.</exception>
	/// <remarks>
	/// This method performs comprehensive data type mapping from DbType to SQL Server native types,
	/// including special handling for version columns (timestamp), date/time kinds, binary kinds,
	/// and length specifications for variable-length types.
	/// </remarks>
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

	/// <summary>
	/// Parses index descriptors from schema column metadata.
	/// </summary>
	/// <param name="schema">The schema containing column definitions.</param>
	/// <returns>A list of index descriptors representing all non-primary-key indexes.</returns>
	/// <remarks>
	/// This method extracts index information from column attributes, grouping columns with
	/// the same index name into multi-column indexes and creating separate descriptors for
	/// single-column indexes. Primary key indexes are excluded as they are handled separately.
	/// </remarks>
	protected static List<IndexDescriptor> ParseIndexes(ISchema schema)
	{
		var result = new List<IndexDescriptor>();

		/*
		 * Process each column to extract index information.
		 */
		foreach (var column in schema.Columns)
		{
			/*
			 * Skip primary key columns as they are handled separately.
			 */
			if (column.IsPrimaryKey)
				continue;

			if (column.IsIndex)
			{
				/*
				 * Single-column index without a named group.
				 */
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
					/*
					 * Multi-column index with a named group.
					 * Find or create the index descriptor for this group.
					 */
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
