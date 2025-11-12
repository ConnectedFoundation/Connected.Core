using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.TypeSystem;
using Connected.Reflection;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Connected.Storage.Sql.Query;

/// <summary>
/// Provides SQL Server specific type system functionality for query translation and data type management.
/// </summary>
/// <remarks>
/// This class handles conversion between CLR types and SQL Server data types, parsing type declarations,
/// and formatting type specifications for SQL Server. It extends <see cref="QueryTypeSystem"/> to provide
/// SQL Server-specific type resolution and formatting capabilities.
/// </remarks>
internal sealed class SqlTypeSystem
	: QueryTypeSystem
{
	/// <summary>
	/// Gets the default maximum size for string data types in SQL Server.
	/// </summary>
	/// <remarks>
	/// Returns <see cref="int.MaxValue"/> to represent VARCHAR(MAX) or NVARCHAR(MAX) columns.
	/// </remarks>
	public static int StringDefaultSize => int.MaxValue;

	/// <summary>
	/// Gets the default maximum size for binary data types in SQL Server.
	/// </summary>
	/// <remarks>
	/// Returns <see cref="int.MaxValue"/> to represent VARBINARY(MAX) columns.
	/// </remarks>
	public static int BinaryDefaultSize => int.MaxValue;

	/// <summary>
	/// Parses a SQL Server type declaration string into a <see cref="DataType"/> object.
	/// </summary>
	/// <param name="typeDeclaration">The SQL type declaration string to parse (e.g., "VARCHAR(50)", "DECIMAL(18,2)").</param>
	/// <returns>A <see cref="DataType"/> instance representing the parsed type.</returns>
	/// <remarks>
	/// This method parses SQL Server type declarations including type name, size parameters, precision, scale,
	/// and nullability constraints. It handles both parameterized types (e.g., VARCHAR(50)) and simple types (e.g., INT).
	/// </remarks>
	public override DataType Parse(string typeDeclaration)
	{
		string[]? args = null;
		string typeName;
		string? remainder = null;
		var openParen = typeDeclaration.IndexOf('(');

		if (openParen >= 0)
		{
			/*
			 * Extract type name before the opening parenthesis
			 */
			typeName = typeDeclaration[..openParen].Trim();

			var closeParen = typeDeclaration.IndexOf(')', openParen);

			if (closeParen < openParen)
				closeParen = typeDeclaration.Length;

			var argstr = typeDeclaration[(openParen + 1)..closeParen];

			/*
			 * Split the arguments by comma for types like DECIMAL(18,2)
			 */
			args = argstr.Split(',');

			remainder = typeDeclaration[(closeParen + 1)..];
		}
		else
		{
			var space = typeDeclaration.IndexOf(' ');

			if (space >= 0)
			{
				typeName = typeDeclaration[..space];
				remainder = typeDeclaration[(space + 1)..].Trim();
			}
			else
				typeName = typeDeclaration;
		}

		/*
		 * Check for NOT NULL constraint in the remainder
		 */
		var isNotNull = remainder is not null && remainder.Contains("NOT NULL", StringComparison.CurrentCultureIgnoreCase);

		return ResolveDataType(typeName, args, isNotNull);
	}

	/// <summary>
	/// Resolves a SQL Server data type from its name and parameters.
	/// </summary>
	/// <param name="typeName">The name of the SQL Server data type.</param>
	/// <param name="args">Optional arguments for the data type (e.g., length, precision, scale).</param>
	/// <param name="isNotNull">Indicates whether the type is non-nullable.</param>
	/// <returns>A <see cref="DataType"/> instance representing the resolved SQL Server type.</returns>
	/// <remarks>
	/// This method handles SQL Server type name aliases (e.g., rowversion to Timestamp, numeric to Decimal)
	/// and extracts appropriate length, precision, and scale values based on the type category.
	/// </remarks>
	public static DataType ResolveDataType(string typeName, string[]? args, bool isNotNull)
	{
		/*
		 * Normalize type name aliases to their standard SQL Server equivalents
		 */
		if (string.Equals(typeName, "rowversion", StringComparison.OrdinalIgnoreCase))
			typeName = "Timestamp";

		if (string.Equals(typeName, "numeric", StringComparison.OrdinalIgnoreCase))
			typeName = "Decimal";

		if (string.Equals(typeName, "sql_variant", StringComparison.OrdinalIgnoreCase))
			typeName = "Variant";

		var dbType = ResolveSqlType(typeName);
		var length = 0;
		short precision = 0;
		short scale = 0;

		switch (dbType)
		{
			case SqlDbType.Binary:
			case SqlDbType.Char:
			case SqlDbType.Image:
			case SqlDbType.NChar:
			case SqlDbType.NVarChar:
			case SqlDbType.VarBinary:
			case SqlDbType.VarChar:
				/*
				 * Parse length argument for character and binary types
				 * Support 'max' keyword for maximum length types
				 */
				length = args is null || args.Length == 0 ? 32 : string.Equals(args[0], "max", StringComparison.OrdinalIgnoreCase) ? int.MaxValue : int.Parse(args[0]);
				break;
			case SqlDbType.Money:
				/*
				 * Set default precision and scale for money type
				 */
				precision = args is null || args.Length == 0 ? (short)29 : short.Parse(args[0], NumberFormatInfo.InvariantInfo);
				scale = args is null || args.Length < 2 ? (short)4 : short.Parse(args[1], NumberFormatInfo.InvariantInfo);
				break;
			case SqlDbType.Decimal:
				/*
				 * Parse precision and optional scale for decimal types
				 */
				precision = args is null || args.Length == 0 ? (short)29 : short.Parse(args[0], NumberFormatInfo.InvariantInfo);
				scale = args is null || args.Length < 2 ? (short)0 : short.Parse(args[1], NumberFormatInfo.InvariantInfo);
				break;
			case SqlDbType.Float:
			case SqlDbType.Real:
				/*
				 * Parse precision for floating-point types
				 */
				precision = args is null || args.Length == 0 ? (short)29 : short.Parse(args[0], NumberFormatInfo.InvariantInfo);
				break;
		}

		return NewType(dbType, isNotNull, length, precision, scale);
	}

	/// <summary>
	/// Creates a new <see cref="SqlDataType"/> instance with the specified characteristics.
	/// </summary>
	/// <param name="type">The SQL Server database type.</param>
	/// <param name="isNotNull">Indicates whether the type is non-nullable.</param>
	/// <param name="length">The maximum length for character or binary types.</param>
	/// <param name="precision">The precision for numeric types.</param>
	/// <param name="scale">The scale for numeric types with decimal places.</param>
	/// <returns>A new <see cref="SqlDataType"/> instance.</returns>
	private static SqlDataType NewType(SqlDbType type, bool isNotNull, int length, short precision, short scale)
	{
		return new SqlDataType(type, isNotNull, length, precision, scale);
	}

	/// <summary>
	/// Resolves a SQL Server data type enumeration value from its string name.
	/// </summary>
	/// <param name="typeName">The name of the SQL Server data type.</param>
	/// <returns>The corresponding <see cref="SqlDbType"/> enumeration value.</returns>
	/// <remarks>
	/// This method performs case-insensitive parsing of the type name to its enumeration equivalent.
	/// </remarks>
	public static SqlDbType ResolveSqlType(string typeName)
	{
		return Enum.Parse<SqlDbType>(typeName, true);
	}

	/// <summary>
	/// Resolves a CLR type to its corresponding SQL Server data type representation.
	/// </summary>
	/// <param name="type">The CLR type to convert.</param>
	/// <returns>A <see cref="DataType"/> representing the appropriate SQL Server data type.</returns>
	/// <remarks>
	/// This method maps common CLR types to their SQL Server equivalents, handling nullable types,
	/// enumerations, and special types like GUID, byte arrays, and date/time types.
	/// </remarks>
	public override DataType ResolveColumnType(Type type)
	{
		var isNotNull = type.GetTypeInfo().IsValueType && !type.IsNullable();
		type = Nullables.GetNonNullableType(type);

		switch (type.GetTypeCode())
		{
			case TypeCode.Boolean:
				return NewType(SqlDbType.Bit, isNotNull, 0, 0, 0);
			case TypeCode.SByte:
			case TypeCode.Byte:
				return NewType(SqlDbType.TinyInt, isNotNull, 0, 0, 0);
			case TypeCode.Int16:
			case TypeCode.UInt16:
				return NewType(SqlDbType.SmallInt, isNotNull, 0, 0, 0);
			case TypeCode.Int32:
			case TypeCode.UInt32:
				return NewType(SqlDbType.Int, isNotNull, 0, 0, 0);
			case TypeCode.Int64:
			case TypeCode.UInt64:
				return NewType(SqlDbType.BigInt, isNotNull, 0, 0, 0);
			case TypeCode.Single:
			case TypeCode.Double:
				return NewType(SqlDbType.Float, isNotNull, 0, 0, 0);
			case TypeCode.String:
				return NewType(SqlDbType.NVarChar, isNotNull, StringDefaultSize, 0, 0);
			case TypeCode.Char:
				return NewType(SqlDbType.NChar, isNotNull, 1, 0, 0);
			case TypeCode.DateTime:
				return NewType(SqlDbType.DateTime, isNotNull, 0, 0, 0);
			case TypeCode.Decimal:
				return NewType(SqlDbType.Decimal, isNotNull, 0, 29, 4);
			default:
				/*
				 * Handle special types not covered by TypeCode
				 */
				if (type == typeof(byte[]))
					return NewType(SqlDbType.VarBinary, isNotNull, BinaryDefaultSize, 0, 0);
				else if (type == typeof(Guid))
					return NewType(SqlDbType.UniqueIdentifier, isNotNull, 0, 0, 0);
				else if (type == typeof(DateTimeOffset))
					return NewType(SqlDbType.DateTimeOffset, isNotNull, 0, 0, 0);
				else if (type == typeof(TimeSpan))
					return NewType(SqlDbType.Time, isNotNull, 0, 0, 0);
				else if (type.GetTypeInfo().IsEnum)
					return NewType(SqlDbType.Int, isNotNull, 0, 0, 0);
				else
					return NewType(SqlDbType.VarBinary, isNotNull, BinaryDefaultSize, 0, 0);
		}
	}

	/// <summary>
	/// Determines whether the specified SQL Server data type has variable length.
	/// </summary>
	/// <param name="dbType">The SQL Server data type to check.</param>
	/// <returns><c>true</c> if the type has variable length; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// Variable-length types include VARCHAR, NVARCHAR, VARBINARY, TEXT, NTEXT, IMAGE, and XML.
	/// </remarks>
	public static bool IsVariableLength(SqlDbType dbType)
	{
		return dbType switch
		{
			SqlDbType.Image or SqlDbType.NText or SqlDbType.NVarChar or SqlDbType.Text or SqlDbType.VarBinary or SqlDbType.VarChar or SqlDbType.Xml => true,
			_ => false,
		};
	}

	/// <summary>
	/// Formats a <see cref="DataType"/> into its SQL Server type declaration string representation.
	/// </summary>
	/// <param name="type">The data type to format.</param>
	/// <param name="suppressSize">Indicates whether to suppress size specifications in the output.</param>
	/// <returns>A string representing the SQL Server type declaration (e.g., "VARCHAR(50)", "DECIMAL(18,2)").</returns>
	/// <remarks>
	/// This method generates SQL Server DDL-compatible type declarations, handling length specifications
	/// for character/binary types and precision/scale for numeric types. When a length equals <see cref="int.MaxValue"/>,
	/// it outputs "(max)" instead of the numeric value.
	/// </remarks>
	public override string Format(DataType type, bool suppressSize)
	{
		var sqlType = (SqlDataType)type;
		var sb = new StringBuilder();

		sb.Append(sqlType.DbType.ToString().ToUpper());

		if (sqlType.Length > 0 && !suppressSize)
		{
			/*
			 * Format length specification, using 'max' for maximum length types
			 */
			if (sqlType.Length == int.MaxValue)
				sb.Append("(max)");
			else
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0})", sqlType.Length);
		}
		else if (sqlType.Precision != 0)
		{
			/*
			 * Format precision and scale for numeric types
			 */
			if (sqlType.Scale != 0)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0},{1})", sqlType.Precision, sqlType.Scale);
			else
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0})", sqlType.Precision);
		}

		return sb.ToString();
	}
}