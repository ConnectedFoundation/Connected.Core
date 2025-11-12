using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.TypeSystem;
using Connected.Reflection;
using Oracle.ManagedDataAccess.Client;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Connected.Storage.Oracle.Query;

/// <summary>
/// Provides Oracle specific type system functionality for query translation and data type management.
/// </summary>
/// <remarks>
/// This class handles conversion between CLR types and Oracle data types, parsing type declarations,
/// and formatting type specifications for Oracle. It extends <see cref="QueryTypeSystem"/> to provide
/// Oracle-specific type resolution and formatting capabilities. Oracle uses a flexible NUMBER type
/// for all numeric values with configurable precision and scale. String types include VARCHAR2 (up to
/// 4000 bytes), NVARCHAR2 (up to 2000 characters), and CLOB for large text. Binary data uses RAW
/// (up to 2000 bytes) and BLOB for large binary objects. The type system handles Oracle-specific
/// features like TIMESTAMP WITH TIME ZONE for DateTimeOffset and RAW(16) for Guid/UUID values.
/// </remarks>
internal sealed class OracleTypeSystem
	: QueryTypeSystem
{
	/// <summary>
	/// Gets the default maximum size for string data types in Oracle.
	/// </summary>
	/// <remarks>
	/// Returns 4000, the maximum length for VARCHAR2 in Oracle databases (in bytes).
	/// For larger text, CLOB type is used which has no practical size limit.
	/// </remarks>
	public static int StringDefaultSize => 4000;

	/// <summary>
	/// Gets the default maximum size for binary data types in Oracle.
	/// </summary>
	/// <remarks>
	/// Returns 2000, the maximum length for RAW in Oracle databases.
	/// For larger binary data, BLOB type is used which has no practical size limit.
	/// </remarks>
	public static int BinaryDefaultSize => 2000;

	/// <summary>
	/// Parses an Oracle type declaration string into a <see cref="DataType"/> object.
	/// </summary>
	/// <param name="typeDeclaration">The Oracle type declaration string to parse (e.g., "VARCHAR2(50)", "NUMBER(18,2)").</param>
	/// <returns>A <see cref="DataType"/> instance representing the parsed type.</returns>
	/// <remarks>
	/// This method parses Oracle type declarations including type name, size parameters, precision, scale,
	/// and nullability constraints. It handles both parameterized types (e.g., VARCHAR2(50)) and simple types
	/// (e.g., NUMBER). Oracle type names are case-insensitive.
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
			 * Split the arguments by comma for types like NUMBER(18,2)
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
	/// Resolves an Oracle data type from its name and parameters.
	/// </summary>
	/// <param name="typeName">The name of the Oracle data type.</param>
	/// <param name="args">Optional arguments for the data type (e.g., length, precision, scale).</param>
	/// <param name="isNotNull">Indicates whether the type is non-nullable.</param>
	/// <returns>A <see cref="DataType"/> instance representing the resolved Oracle type.</returns>
	/// <remarks>
	/// This method handles Oracle type name aliases and extracts appropriate length, precision,
	/// and scale values based on the type category. Oracle's NUMBER type is used for all numeric
	/// values with configurable precision and scale.
	/// </remarks>
	public static DataType ResolveDataType(string typeName, string[]? args, bool isNotNull)
	{
		/*
		 * Normalize type name to standard Oracle type
		 */
		typeName = NormalizeTypeName(typeName);

		var dbType = ResolveOracleType(typeName);
		var length = 0;
		short precision = 0;
		short scale = 0;

		switch (dbType)
		{
			case OracleDbType.Varchar2:
			case OracleDbType.NVarchar2:
			case OracleDbType.Char:
			case OracleDbType.NChar:
			case OracleDbType.Raw:
				/*
				 * Parse length argument for character and binary types
				 */
				length = args is null || args.Length == 0 ? StringDefaultSize : int.Parse(args[0]);
				break;
			case OracleDbType.Decimal:
				/*
				 * Parse precision and optional scale for numeric types
				 * Oracle NUMBER type can store up to 38 digits of precision
				 */
				precision = args is null || args.Length == 0 ? (short)38 : short.Parse(args[0], NumberFormatInfo.InvariantInfo);
				scale = args is null || args.Length < 2 ? (short)0 : short.Parse(args[1], NumberFormatInfo.InvariantInfo);
				break;
			case OracleDbType.Double:
			case OracleDbType.Single:
				/*
				 * Parse precision for floating-point types (less common in Oracle)
				 */
				if (args is not null && args.Length > 0)
					precision = short.Parse(args[0], NumberFormatInfo.InvariantInfo);
				break;
		}

		return NewType(dbType, isNotNull, length, precision, scale);
	}

	/// <summary>
	/// Normalizes Oracle type name aliases to standard names.
	/// </summary>
	/// <param name="typeName">The type name to normalize.</param>
	/// <returns>The normalized type name.</returns>
	private static string NormalizeTypeName(string typeName)
	{
		return typeName.ToLowerInvariant() switch
		{
			"number" => "decimal",
			"integer" => "int32",
			"int" => "int32",
			"smallint" => "int16",
			"float" => "double",
			"binary_float" => "single",
			"binary_double" => "double",
			"varchar" => "varchar2",
			"nvarchar" => "nvarchar2",
			"long" => "clob",
			"long raw" => "blob",
			_ => typeName.ToLowerInvariant()
		};
	}

	/// <summary>
	/// Creates a new <see cref="OracleDataType"/> instance with the specified characteristics.
	/// </summary>
	/// <param name="type">The Oracle database type.</param>
	/// <param name="isNotNull">Indicates whether the type is non-nullable.</param>
	/// <param name="length">The maximum length for character or binary types.</param>
	/// <param name="precision">The precision for numeric types.</param>
	/// <param name="scale">The scale for numeric types with decimal places.</param>
	/// <returns>A new <see cref="OracleDataType"/> instance.</returns>
	private static OracleDataType NewType(OracleDbType type, bool isNotNull, int length, short precision, short scale)
	{
		return new OracleDataType(type, isNotNull, length, precision, scale);
	}

	/// <summary>
	/// Resolves an Oracle data type enumeration value from its string name.
	/// </summary>
	/// <param name="typeName">The name of the Oracle data type.</param>
	/// <returns>The corresponding <see cref="OracleDbType"/> enumeration value.</returns>
	/// <remarks>
	/// This method maps Oracle type names to OracleDbType enumeration values.
	/// </remarks>
	public static OracleDbType ResolveOracleType(string typeName)
	{
		return typeName.ToLowerInvariant() switch
		{
			"int16" => OracleDbType.Int16,
			"int32" => OracleDbType.Int32,
			"int64" => OracleDbType.Int64,
			"decimal" => OracleDbType.Decimal,
			"single" => OracleDbType.Single,
			"double" => OracleDbType.Double,
			"varchar2" => OracleDbType.Varchar2,
			"nvarchar2" => OracleDbType.NVarchar2,
			"char" => OracleDbType.Char,
			"nchar" => OracleDbType.NChar,
			"clob" => OracleDbType.Clob,
			"nclob" => OracleDbType.NClob,
			"blob" => OracleDbType.Blob,
			"raw" => OracleDbType.Raw,
			"date" => OracleDbType.Date,
			"timestamp" => OracleDbType.TimeStamp,
			"timestamptz" => OracleDbType.TimeStampTZ,
			"interval day to second" => OracleDbType.IntervalDS,
			"interval year to month" => OracleDbType.IntervalYM,
			_ => OracleDbType.Varchar2
		};
	}

	/// <summary>
	/// Resolves a CLR type to its corresponding Oracle data type representation.
	/// </summary>
	/// <param name="type">The CLR type to convert.</param>
	/// <returns>A <see cref="DataType"/> representing the appropriate Oracle data type.</returns>
	/// <remarks>
	/// This method maps common CLR types to their Oracle equivalents, handling nullable types,
	/// enumerations, and special types like GUID, byte arrays, and date/time types. Oracle uses
	/// NUMBER for all numeric types and RAW(16) for Guid/UUID values.
	/// </remarks>
	public override DataType ResolveColumnType(Type type)
	{
		var isNotNull = type.GetTypeInfo().IsValueType && !type.IsNullable();
		type = Nullables.GetNonNullableType(type);

		switch (type.GetTypeCode())
		{
			case TypeCode.Boolean:
				return NewType(OracleDbType.Int16, isNotNull, 0, 1, 0);
			case TypeCode.SByte:
			case TypeCode.Byte:
				return NewType(OracleDbType.Int16, isNotNull, 0, 3, 0);
			case TypeCode.Int16:
			case TypeCode.UInt16:
				return NewType(OracleDbType.Int16, isNotNull, 0, 5, 0);
			case TypeCode.Int32:
			case TypeCode.UInt32:
				return NewType(OracleDbType.Int32, isNotNull, 0, 10, 0);
			case TypeCode.Int64:
			case TypeCode.UInt64:
				return NewType(OracleDbType.Int64, isNotNull, 0, 19, 0);
			case TypeCode.Single:
				return NewType(OracleDbType.Single, isNotNull, 0, 0, 0);
			case TypeCode.Double:
				return NewType(OracleDbType.Double, isNotNull, 0, 0, 0);
			case TypeCode.String:
				return NewType(OracleDbType.Varchar2, isNotNull, StringDefaultSize, 0, 0);
			case TypeCode.Char:
				return NewType(OracleDbType.Char, isNotNull, 1, 0, 0);
			case TypeCode.DateTime:
				return NewType(OracleDbType.Date, isNotNull, 0, 0, 0);
			case TypeCode.Decimal:
				return NewType(OracleDbType.Decimal, isNotNull, 0, 29, 4);
			default:
				/*
				 * Handle special types not covered by TypeCode
				 */
				if (type == typeof(byte[]))
					return NewType(OracleDbType.Blob, isNotNull, BinaryDefaultSize, 0, 0);
				else if (type == typeof(Guid))
					return NewType(OracleDbType.Raw, isNotNull, 16, 0, 0);
				else if (type == typeof(DateTimeOffset))
					return NewType(OracleDbType.TimeStampTZ, isNotNull, 0, 0, 0);
				else if (type == typeof(TimeSpan))
					return NewType(OracleDbType.IntervalDS, isNotNull, 0, 0, 0);
				else if (type.GetTypeInfo().IsEnum)
					return NewType(OracleDbType.Int32, isNotNull, 0, 10, 0);
				else
					return NewType(OracleDbType.Blob, isNotNull, BinaryDefaultSize, 0, 0);
		}
	}

	/// <summary>
	/// Determines whether the specified Oracle data type has variable length.
	/// </summary>
	/// <param name="dbType">The Oracle data type to check.</param>
	/// <returns><c>true</c> if the type has variable length; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// Variable-length types in Oracle include VARCHAR2, NVARCHAR2, RAW, CLOB, NCLOB, and BLOB.
	/// Fixed-length types include NUMBER, DATE, TIMESTAMP, and CHAR.
	/// </remarks>
	public static bool IsVariableLength(OracleDbType dbType)
	{
		return dbType switch
		{
			OracleDbType.Varchar2 or OracleDbType.NVarchar2 or OracleDbType.Raw or OracleDbType.Clob or OracleDbType.NClob or OracleDbType.Blob => true,
			_ => false,
		};
	}

	/// <summary>
	/// Formats a <see cref="DataType"/> into its Oracle type declaration string representation.
	/// </summary>
	/// <param name="type">The data type to format.</param>
	/// <param name="suppressSize">Indicates whether to suppress size specifications in the output.</param>
	/// <returns>A string representing the Oracle type declaration (e.g., "VARCHAR2(50)", "NUMBER(18,2)").</returns>
	/// <remarks>
	/// This method generates Oracle DDL-compatible type declarations, handling length specifications
	/// for character/binary types and precision/scale for numeric types. Oracle uses VARCHAR2 for
	/// variable-length strings and NUMBER for all numeric types with configurable precision and scale.
	/// </remarks>
	public override string Format(DataType type, bool suppressSize)
	{
		var oracleType = (OracleDataType)type;
		var sb = new StringBuilder();

		/*
		 * Get Oracle type name
		 */
		sb.Append(GetTypeName(oracleType.DbType));

		if (oracleType.Length > 0 && !suppressSize)
		{
			/*
			 * Format length specification for character and binary types
			 */
			sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0})", oracleType.Length);
		}
		else if (oracleType.Precision != 0)
		{
			/*
			 * Format precision and scale for numeric types
			 */
			if (oracleType.Scale != 0)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0},{1})", oracleType.Precision, oracleType.Scale);
			else
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0})", oracleType.Precision);
		}

		return sb.ToString();
	}

	/// <summary>
	/// Gets the Oracle type name string for the specified OracleDbType.
	/// </summary>
	/// <param name="dbType">The OracleDbType to get the name for.</param>
	/// <returns>The Oracle type name in uppercase.</returns>
	private static string GetTypeName(OracleDbType dbType)
	{
		return dbType switch
		{
			OracleDbType.Int16 => "NUMBER",
			OracleDbType.Int32 => "NUMBER",
			OracleDbType.Int64 => "NUMBER",
			OracleDbType.Decimal => "NUMBER",
			OracleDbType.Single => "BINARY_FLOAT",
			OracleDbType.Double => "BINARY_DOUBLE",
			OracleDbType.Varchar2 => "VARCHAR2",
			OracleDbType.NVarchar2 => "NVARCHAR2",
			OracleDbType.Char => "CHAR",
			OracleDbType.NChar => "NCHAR",
			OracleDbType.Clob => "CLOB",
			OracleDbType.NClob => "NCLOB",
			OracleDbType.Blob => "BLOB",
			OracleDbType.Raw => "RAW",
			OracleDbType.Date => "DATE",
			OracleDbType.TimeStamp => "TIMESTAMP",
			OracleDbType.TimeStampTZ => "TIMESTAMP WITH TIME ZONE",
			OracleDbType.IntervalDS => "INTERVAL DAY TO SECOND",
			OracleDbType.IntervalYM => "INTERVAL YEAR TO MONTH",
			_ => "VARCHAR2"
		};
	}
}
