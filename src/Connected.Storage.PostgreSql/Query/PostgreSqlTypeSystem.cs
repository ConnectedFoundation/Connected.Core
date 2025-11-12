using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.TypeSystem;
using Connected.Reflection;
using NpgsqlTypes;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Connected.Storage.PostgreSql.Query;

/// <summary>
/// Provides PostgreSQL specific type system functionality for query translation and data type management.
/// </summary>
/// <remarks>
/// This class handles conversion between CLR types and PostgreSQL data types, parsing type declarations,
/// and formatting type specifications for PostgreSQL. It extends <see cref="QueryTypeSystem"/> to provide
/// PostgreSQL-specific type resolution and formatting capabilities. The implementation uses NpgsqlDbType
/// for native PostgreSQL type representation and handles PostgreSQL-specific features like arrays, JSON,
/// and unlimited text types.
/// </remarks>
internal sealed class PostgreSqlTypeSystem
	: QueryTypeSystem
{
	/// <summary>
	/// Gets the default maximum size for string data types in PostgreSQL.
	/// </summary>
	/// <remarks>
	/// Returns <see cref="int.MaxValue"/> to represent TEXT or VARCHAR without size limit.
	/// PostgreSQL allows unlimited text storage with the TEXT type.
	/// </remarks>
	public static int StringDefaultSize => int.MaxValue;

	/// <summary>
	/// Gets the default maximum size for binary data types in PostgreSQL.
	/// </summary>
	/// <remarks>
	/// Returns <see cref="int.MaxValue"/> to represent BYTEA columns without size limit.
	/// PostgreSQL BYTEA type has no practical size limit beyond storage capacity.
	/// </remarks>
	public static int BinaryDefaultSize => int.MaxValue;

	/// <summary>
	/// Parses a PostgreSQL type declaration string into a <see cref="DataType"/> object.
	/// </summary>
	/// <param name="typeDeclaration">The PostgreSQL type declaration string to parse (e.g., "VARCHAR(50)", "NUMERIC(18,2)").</param>
	/// <returns>A <see cref="DataType"/> instance representing the parsed type.</returns>
	/// <remarks>
	/// This method parses PostgreSQL type declarations including type name, size parameters, precision, scale,
	/// and nullability constraints. It handles both parameterized types (e.g., VARCHAR(50)) and simple types (e.g., INTEGER).
	/// PostgreSQL type names are case-insensitive and the method handles common aliases.
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
			 * Split the arguments by comma for types like NUMERIC(18,2)
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
	/// Resolves a PostgreSQL data type from its name and parameters.
	/// </summary>
	/// <param name="typeName">The name of the PostgreSQL data type.</param>
	/// <param name="args">Optional arguments for the data type (e.g., length, precision, scale).</param>
	/// <param name="isNotNull">Indicates whether the type is non-nullable.</param>
	/// <returns>A <see cref="DataType"/> instance representing the resolved PostgreSQL type.</returns>
	/// <remarks>
	/// This method handles PostgreSQL type name aliases (e.g., int4 to Integer, int8 to Bigint, bool to Boolean)
	/// and extracts appropriate length, precision, and scale values based on the type category. It supports
	/// PostgreSQL-specific types like UUID, JSONB, and BYTEA.
	/// </remarks>
	public static DataType ResolveDataType(string typeName, string[]? args, bool isNotNull)
	{
		/*
		 * Normalize type name aliases to their standard PostgreSQL equivalents
		 */
		typeName = NormalizeTypeName(typeName);

		var dbType = ResolvePostgreSqlType(typeName);
		var length = 0;
		short precision = 0;
		short scale = 0;

		switch (dbType)
		{
			case NpgsqlDbType.Varchar:
			case NpgsqlDbType.Char:
			case NpgsqlDbType.Bytea:
				/*
				 * Parse length argument for character and binary types
				 * PostgreSQL doesn't have 'max' keyword, unlimited TEXT is used instead
				 */
				length = args is null || args.Length == 0 ? StringDefaultSize : int.Parse(args[0]);
				break;
			case NpgsqlDbType.Numeric:
				/*
				 * Parse precision and optional scale for numeric types
				 */
				precision = args is null || args.Length == 0 ? (short)29 : short.Parse(args[0], NumberFormatInfo.InvariantInfo);
				scale = args is null || args.Length < 2 ? (short)0 : short.Parse(args[1], NumberFormatInfo.InvariantInfo);
				break;
			case NpgsqlDbType.Real:
			case NpgsqlDbType.Double:
				/*
				 * Parse precision for floating-point types (optional in PostgreSQL)
				 */
				if (args is not null && args.Length > 0)
					precision = short.Parse(args[0], NumberFormatInfo.InvariantInfo);
				break;
		}

		return NewType(dbType, isNotNull, length, precision, scale);
	}

	/// <summary>
	/// Normalizes PostgreSQL type name aliases to standard names.
	/// </summary>
	/// <param name="typeName">The type name to normalize.</param>
	/// <returns>The normalized type name.</returns>
	private static string NormalizeTypeName(string typeName)
	{
		return typeName.ToLowerInvariant() switch
		{
			"int2" => "smallint",
			"int4" => "integer",
			"int8" => "bigint",
			"float4" => "real",
			"float8" => "double precision",
			"bool" => "boolean",
			"character varying" => "varchar",
			"character" => "char",
			"timestamp without time zone" => "timestamp",
			"timestamp with time zone" => "timestamptz",
			"time without time zone" => "time",
			"time with time zone" => "timetz",
			_ => typeName.ToLowerInvariant()
		};
	}

	/// <summary>
	/// Creates a new <see cref="PostgreSqlDataType"/> instance with the specified characteristics.
	/// </summary>
	/// <param name="type">The PostgreSQL database type.</param>
	/// <param name="isNotNull">Indicates whether the type is non-nullable.</param>
	/// <param name="length">The maximum length for character or binary types.</param>
	/// <param name="precision">The precision for numeric types.</param>
	/// <param name="scale">The scale for numeric types with decimal places.</param>
	/// <returns>A new <see cref="PostgreSqlDataType"/> instance.</returns>
	private static PostgreSqlDataType NewType(NpgsqlDbType type, bool isNotNull, int length, short precision, short scale)
	{
		return new PostgreSqlDataType(type, isNotNull, length, precision, scale);
	}

	/// <summary>
	/// Resolves a PostgreSQL data type enumeration value from its string name.
	/// </summary>
	/// <param name="typeName">The name of the PostgreSQL data type.</param>
	/// <returns>The corresponding <see cref="NpgsqlDbType"/> enumeration value.</returns>
	/// <remarks>
	/// This method maps PostgreSQL type names to NpgsqlDbType enumeration values.
	/// It handles both standard PostgreSQL types and common aliases.
	/// </remarks>
	public static NpgsqlDbType ResolvePostgreSqlType(string typeName)
	{
		return typeName.ToLowerInvariant() switch
		{
			"smallint" => NpgsqlDbType.Smallint,
			"integer" => NpgsqlDbType.Integer,
			"bigint" => NpgsqlDbType.Bigint,
			"real" => NpgsqlDbType.Real,
			"double precision" => NpgsqlDbType.Double,
			"numeric" => NpgsqlDbType.Numeric,
			"decimal" => NpgsqlDbType.Numeric,
			"money" => NpgsqlDbType.Money,
			"varchar" => NpgsqlDbType.Varchar,
			"char" => NpgsqlDbType.Char,
			"text" => NpgsqlDbType.Text,
			"bytea" => NpgsqlDbType.Bytea,
			"timestamp" => NpgsqlDbType.Timestamp,
			"timestamptz" => NpgsqlDbType.TimestampTz,
			"date" => NpgsqlDbType.Date,
			"time" => NpgsqlDbType.Time,
			"timetz" => NpgsqlDbType.TimeTz,
			"interval" => NpgsqlDbType.Interval,
			"boolean" => NpgsqlDbType.Boolean,
			"uuid" => NpgsqlDbType.Uuid,
			"json" => NpgsqlDbType.Json,
			"jsonb" => NpgsqlDbType.Jsonb,
			"xml" => NpgsqlDbType.Xml,
			"point" => NpgsqlDbType.Point,
			"line" => NpgsqlDbType.Line,
			"lseg" => NpgsqlDbType.LSeg,
			"box" => NpgsqlDbType.Box,
			"path" => NpgsqlDbType.Path,
			"polygon" => NpgsqlDbType.Polygon,
			"circle" => NpgsqlDbType.Circle,
			"inet" => NpgsqlDbType.Inet,
			"cidr" => NpgsqlDbType.Cidr,
			"macaddr" => NpgsqlDbType.MacAddr,
			"bit" => NpgsqlDbType.Bit,
			"varbit" => NpgsqlDbType.Varbit,
			_ => NpgsqlDbType.Text
		};
	}

	/// <summary>
	/// Resolves a CLR type to its corresponding PostgreSQL data type representation.
	/// </summary>
	/// <param name="type">The CLR type to convert.</param>
	/// <returns>A <see cref="DataType"/> representing the appropriate PostgreSQL data type.</returns>
	/// <remarks>
	/// This method maps common CLR types to their PostgreSQL equivalents, handling nullable types,
	/// enumerations, and special types like GUID, byte arrays, and date/time types. PostgreSQL-specific
	/// mappings include UUID for Guid and TIMESTAMPTZ for DateTimeOffset to preserve timezone information.
	/// </remarks>
	public override DataType ResolveColumnType(Type type)
	{
		var isNotNull = type.GetTypeInfo().IsValueType && !type.IsNullable();
		type = Nullables.GetNonNullableType(type);

		switch (type.GetTypeCode())
		{
			case TypeCode.Boolean:
				return NewType(NpgsqlDbType.Boolean, isNotNull, 0, 0, 0);
			case TypeCode.SByte:
			case TypeCode.Byte:
				return NewType(NpgsqlDbType.Smallint, isNotNull, 0, 0, 0);
			case TypeCode.Int16:
			case TypeCode.UInt16:
				return NewType(NpgsqlDbType.Smallint, isNotNull, 0, 0, 0);
			case TypeCode.Int32:
			case TypeCode.UInt32:
				return NewType(NpgsqlDbType.Integer, isNotNull, 0, 0, 0);
			case TypeCode.Int64:
			case TypeCode.UInt64:
				return NewType(NpgsqlDbType.Bigint, isNotNull, 0, 0, 0);
			case TypeCode.Single:
				return NewType(NpgsqlDbType.Real, isNotNull, 0, 0, 0);
			case TypeCode.Double:
				return NewType(NpgsqlDbType.Double, isNotNull, 0, 0, 0);
			case TypeCode.String:
				return NewType(NpgsqlDbType.Text, isNotNull, StringDefaultSize, 0, 0);
			case TypeCode.Char:
				return NewType(NpgsqlDbType.Char, isNotNull, 1, 0, 0);
			case TypeCode.DateTime:
				return NewType(NpgsqlDbType.Timestamp, isNotNull, 0, 0, 0);
			case TypeCode.Decimal:
				return NewType(NpgsqlDbType.Numeric, isNotNull, 0, 29, 4);
			default:
				/*
				 * Handle special types not covered by TypeCode
				 */
				if (type == typeof(byte[]))
					return NewType(NpgsqlDbType.Bytea, isNotNull, BinaryDefaultSize, 0, 0);
				else if (type == typeof(Guid))
					return NewType(NpgsqlDbType.Uuid, isNotNull, 0, 0, 0);
				else if (type == typeof(DateTimeOffset))
					return NewType(NpgsqlDbType.TimestampTz, isNotNull, 0, 0, 0);
				else if (type == typeof(TimeSpan))
					return NewType(NpgsqlDbType.Interval, isNotNull, 0, 0, 0);
				else if (type.GetTypeInfo().IsEnum)
					return NewType(NpgsqlDbType.Integer, isNotNull, 0, 0, 0);
				else
					return NewType(NpgsqlDbType.Bytea, isNotNull, BinaryDefaultSize, 0, 0);
		}
	}

	/// <summary>
	/// Determines whether the specified PostgreSQL data type has variable length.
	/// </summary>
	/// <param name="dbType">The PostgreSQL data type to check.</param>
	/// <returns><c>true</c> if the type has variable length; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// Variable-length types in PostgreSQL include VARCHAR, TEXT, BYTEA, JSON, JSONB, and XML.
	/// Fixed-length types like INTEGER, BIGINT, TIMESTAMP do not have variable length.
	/// </remarks>
	public static bool IsVariableLength(NpgsqlDbType dbType)
	{
		return dbType switch
		{
			NpgsqlDbType.Text or NpgsqlDbType.Varchar or NpgsqlDbType.Bytea or NpgsqlDbType.Json or NpgsqlDbType.Jsonb or NpgsqlDbType.Xml => true,
			_ => false,
		};
	}

	/// <summary>
	/// Formats a <see cref="DataType"/> into its PostgreSQL type declaration string representation.
	/// </summary>
	/// <param name="type">The data type to format.</param>
	/// <param name="suppressSize">Indicates whether to suppress size specifications in the output.</param>
	/// <returns>A string representing the PostgreSQL type declaration (e.g., "VARCHAR(50)", "NUMERIC(18,2)").</returns>
	/// <remarks>
	/// This method generates PostgreSQL DDL-compatible type declarations, handling length specifications
	/// for character/binary types and precision/scale for numeric types. Unlike SQL Server, PostgreSQL
	/// uses TEXT type for unlimited text storage instead of VARCHAR(MAX). The method uses lowercase
	/// type names following PostgreSQL conventions.
	/// </remarks>
	public override string Format(DataType type, bool suppressSize)
	{
		var pgType = (PostgreSqlDataType)type;
		var sb = new StringBuilder();

		/*
		 * Get PostgreSQL type name (lowercase convention)
		 */
		sb.Append(GetTypeName(pgType.DbType));

		if (pgType.Length > 0 && !suppressSize && pgType.DbType != NpgsqlDbType.Text)
		{
			/*
			 * Format length specification for types that support it
			 * TEXT type doesn't need length specification
			 */
			if (pgType.Length == int.MaxValue)
			{
				/*
				 * PostgreSQL uses TEXT for unlimited length instead of VARCHAR(MAX)
				 */
				return "TEXT";
			}

			sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0})", pgType.Length);
		}
		else if (pgType.Precision != 0)
		{
			/*
			 * Format precision and scale for numeric types
			 */
			if (pgType.Scale != 0)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0},{1})", pgType.Precision, pgType.Scale);
			else
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0})", pgType.Precision);
		}

		return sb.ToString();
	}

	/// <summary>
	/// Gets the PostgreSQL type name string for the specified NpgsqlDbType.
	/// </summary>
	/// <param name="dbType">The NpgsqlDbType to get the name for.</param>
	/// <returns>The PostgreSQL type name in lowercase.</returns>
	private static string GetTypeName(NpgsqlDbType dbType)
	{
		return dbType switch
		{
			NpgsqlDbType.Smallint => "smallint",
			NpgsqlDbType.Integer => "integer",
			NpgsqlDbType.Bigint => "bigint",
			NpgsqlDbType.Real => "real",
			NpgsqlDbType.Double => "double precision",
			NpgsqlDbType.Numeric => "numeric",
			NpgsqlDbType.Money => "money",
			NpgsqlDbType.Varchar => "varchar",
			NpgsqlDbType.Char => "char",
			NpgsqlDbType.Text => "text",
			NpgsqlDbType.Bytea => "bytea",
			NpgsqlDbType.Timestamp => "timestamp",
			NpgsqlDbType.TimestampTz => "timestamptz",
			NpgsqlDbType.Date => "date",
			NpgsqlDbType.Time => "time",
			NpgsqlDbType.TimeTz => "timetz",
			NpgsqlDbType.Interval => "interval",
			NpgsqlDbType.Boolean => "boolean",
			NpgsqlDbType.Uuid => "uuid",
			NpgsqlDbType.Json => "json",
			NpgsqlDbType.Jsonb => "jsonb",
			NpgsqlDbType.Xml => "xml",
			_ => "text"
		};
	}
}
