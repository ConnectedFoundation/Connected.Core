using Connected.Storage.Schemas;
using System.Data;
using System.Text;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Base class for Oracle schema synchronization commands.
/// </summary>
/// <remarks>
/// This abstract class provides common functionality for all schema operations including
/// SQL identifier escaping, type formatting, and column definition generation. It ensures
/// consistent Oracle syntax across all schema modification operations and handles
/// Oracle-specific requirements such as double-quote identifier escaping (for case-sensitive
/// identifiers), NUMBER type with precision/scale, and VARCHAR2/CLOB string types. Oracle
/// identifiers are case-insensitive and fold to uppercase when unquoted, but case-sensitive
/// when enclosed in double quotes. The class serves as the foundation for both transaction
/// (write) and query (read) operations against the Oracle schema.
/// </remarks>
internal abstract class SynchronizationCommand
{
	/// <summary>
	/// Escapes an Oracle identifier using double quotes.
	/// </summary>
	/// <param name="identifier">The identifier to escape.</param>
	/// <returns>The escaped identifier in the format "identifier".</returns>
	/// <remarks>
	/// Oracle identifiers are case-insensitive when unquoted (folding to uppercase) and
	/// case-sensitive when quoted. Double quotes are required for identifiers containing
	/// special characters, spaces, or reserved keywords, or when case sensitivity is needed.
	/// </remarks>
	protected static string Escape(string identifier)
	{
		return $"\"{identifier}\"";
	}

	/// <summary>
	/// Escapes a schema-qualified table name for Oracle.
	/// </summary>
	/// <param name="schema">The schema name (optional, can be null for current user schema).</param>
	/// <param name="table">The table name.</param>
	/// <returns/The fully qualified and escaped table name.</returns>
	/// <remarks>
	/// Creates a fully qualified identifier in the format "schema"."table" when schema is provided,
	/// or just "table" when schema is null/empty. Oracle treats schema and user as synonymous.
	/// </remarks>
	protected static string Escape(string? schema, string table)
	{
		if (string.IsNullOrWhiteSpace(schema))
			return Escape(table);

		return $"{Escape(schema)}.{Escape(table)}";
	}

	/// <summary>
	/// Formats an Oracle data type string with optional size parameters.
	/// </summary>
	/// <param name="dataType">The DbType to format.</param>
	/// <param name="size">The size parameter for the type, or 0 if not applicable.</param>
	/// <returns/The formatted type string (e.g., "VARCHAR2(50)", "NUMBER(18,2)").</returns>
	/// <remarks>
	/// Maps .NET DbType values to their Oracle equivalents. Oracle uses NUMBER for all numeric
	/// types with configurable precision and scale, VARCHAR2/CLOB for strings, and RAW/BLOB
	/// for binary data. The method handles size specifications appropriately for each type.
	/// </remarks>
	protected static string FormatType(DbType dataType, int size)
	{
		var typeName = MapDbTypeToOracleType(dataType);

		if (RequiresLength(dataType) && size > 0)
			return $"{typeName}({size})";

		return typeName;
	}

	/// <summary>
	/// Maps DbType to Oracle type names.
	/// </summary>
	/// <param name="dbType">The DbType to map.</param>
	/// <returns/The Oracle type name.</returns>
	/// <remarks>
	/// Oracle uses a different type system than PostgreSQL or SQL Server. Key differences include:
	/// - NUMBER for all numeric types (with precision/scale)
	/// - VARCHAR2 instead of VARCHAR (Oracle's proprietary type)
	/// - CLOB for large text instead of TEXT
	/// - RAW/BLOB for binary data
	/// - DATE for date/time (without time zone)
	/// - TIMESTAMP WITH TIME ZONE for date/time with time zone
	/// </remarks>
	private static string MapDbTypeToOracleType(DbType dbType)
	{
		return dbType switch
		{
			DbType.AnsiString => "VARCHAR2",
			DbType.Binary => "BLOB",
			DbType.Byte => "NUMBER(3)",
			DbType.Boolean => "NUMBER(1)",
			DbType.Currency => "NUMBER(19,4)",
			DbType.Date => "DATE",
			DbType.DateTime => "DATE",
			DbType.DateTime2 => "TIMESTAMP",
			DbType.DateTimeOffset => "TIMESTAMP WITH TIME ZONE",
			DbType.Decimal => "NUMBER(29,4)",
			DbType.Double => "BINARY_DOUBLE",
			DbType.Guid => "RAW(16)",
			DbType.Int16 => "NUMBER(5)",
			DbType.Int32 => "NUMBER(10)",
			DbType.Int64 => "NUMBER(19)",
			DbType.Object => "BLOB",
			DbType.SByte => "NUMBER(3)",
			DbType.Single => "BINARY_FLOAT",
			DbType.String => "VARCHAR2",
			DbType.Time => "INTERVAL DAY TO SECOND",
			DbType.UInt16 => "NUMBER(5)",
			DbType.UInt32 => "NUMBER(10)",
			DbType.UInt64 => "NUMBER(19)",
			DbType.VarNumeric => "NUMBER",
			DbType.AnsiStringFixedLength => "CHAR",
			DbType.StringFixedLength => "NCHAR",
			DbType.Xml => "CLOB",
			_ => "VARCHAR2"
		};
	}

	/// <summary>
	/// Determines if a DbType requires length specification.
	/// </summary>
	/// <param name="dbType">The DbType to check.</param>
	/// <returns><c>true</c> if length is required; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// Variable-length types in Oracle (VARCHAR2, NVARCHAR2, RAW, CHAR, NCHAR) require
	/// length specification. Fixed-length types (NUMBER, DATE, TIMESTAMP, CLOB, BLOB) do not.
	/// </remarks>
	private static bool RequiresLength(DbType dbType)
	{
		return dbType switch
		{
			DbType.AnsiString => true,
			DbType.String => true,
			DbType.AnsiStringFixedLength => true,
			DbType.StringFixedLength => true,
			_ => false
		};
	}

	/// <summary>
	/// Creates the column definition SQL for a schema column.
	/// </summary>
	/// <param name="column">The schema column to generate definition for.</param>
	/// <returns/The column definition SQL fragment.</returns>
	/// <remarks>
	/// Generates Oracle-compatible column definition including column name, data type,
	/// nullability, default value, and identity specification. Oracle 12c+ supports
	/// GENERATED AS IDENTITY for auto-increment columns. For earlier versions, sequences
	/// and triggers must be used separately. The method escapes identifiers with double
	/// quotes to preserve case sensitivity and handle special characters.
	/// </remarks>
	protected static string CreateColumnCommandText(ISchemaColumn column)
	{
		var sb = new StringBuilder();

		/*
		 * Start with escaped column name
		 */
		sb.Append(Escape(column.Name));
		sb.Append(' ');

		/*
		 * Add data type with size if applicable
		 */
		sb.Append(FormatType(column.DataType, column.MaxLength));

		/*
		 * Handle identity columns (Oracle 12c+ feature)
		 */
		if (column.IsIdentity)
		{
			/*
			 * Oracle 12c+ supports GENERATED BY DEFAULT AS IDENTITY
			 * For earlier versions, sequences and triggers must be used
			 */
			sb.Append(" GENERATED BY DEFAULT AS IDENTITY");
		}

		/*
		 * Add default value if specified and not an identity column
		 */
		if (!string.IsNullOrWhiteSpace(column.DefaultValue) && !column.IsIdentity)
		{
			sb.Append(" DEFAULT ");
			sb.Append(column.DefaultValue);
		}

		/*
		 * Add nullability constraint
		 * Oracle uses NOT NULL instead of PostgreSQL's NOT NULL or SQL Server's NULL/NOT NULL
		 */
		if (!column.IsNullable)
			sb.Append(" NOT NULL");

		return sb.ToString();
	}

	/// <summary>
	/// Parses index definitions from schema column attributes.
	/// </summary>
	/// <param name="schema">The schema definition containing columns with index attributes.</param>
	/// <returns>A list of index descriptors extracted from column metadata.</returns>
	/// <remarks>
	/// Extracts index information from column attributes and creates index descriptors for
	/// later index creation operations. Oracle supports various index types including B-tree
	/// (default), bitmap, function-based, and domain indexes. The method groups columns by
	/// index name to support composite indexes spanning multiple columns.
	/// </remarks>
	protected static List<IndexDescriptor> ParseIndexes(ISchema schema)
	{
		var result = new List<IndexDescriptor>();

		/*
		 * Iterate through columns and collect index definitions based on IsIndex flag
		 */
		foreach (var column in schema.Columns)
		{
			if (!column.IsIndex)
				continue;

			/*
			 * Use column's Index property (string name) or generate default name
			 */
			var indexName = string.IsNullOrWhiteSpace(column.Index) 
				? $"IX_{schema.Name}_{column.Name}" 
				: column.Index;

			var existing = result.FirstOrDefault(f => string.Equals(f.Name, indexName, StringComparison.OrdinalIgnoreCase));

			if (existing is not null)
			{
				/*
				 * Add column to existing composite index
				 */
				existing.Columns.Add(column.Name);
			}
			else
			{
				/*
				 * Create new index descriptor
				 */
				result.Add(new IndexDescriptor
				{
					Name = indexName,
					Schema = schema.Schema,
					TableName = schema.Name,
					IsUnique = column.IsUnique,
					Columns = [column.Name]
				});
			}
		}

		return result;
	}
}
