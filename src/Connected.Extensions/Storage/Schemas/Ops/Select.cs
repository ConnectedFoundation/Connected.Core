using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Reflection;
using Connected.Services;
using System.Data;
using System.Globalization;
using System.Reflection;

namespace Connected.Storage.Schemas.Ops;

/// <summary>
/// Selects and constructs a schema definition from an entity type.
/// </summary>
/// <remarks>
/// This operation analyzes an entity type's metadata through reflection and builds a complete
/// schema representation including table information, columns, constraints, indexes, and type
/// mappings. It processes property attributes to determine database column characteristics such
/// as data types, nullability, precision, and default values. The resulting schema is used by
/// storage providers for schema synchronization, validation, and DDL generation operations.
/// </remarks>
internal sealed class Select
	: ServiceFunction<ISelectSchemaDto, ISchema?>
{
	/// <inheritdoc/>
	protected override Task<ISchema?> OnInvoke()
	{
		/*
		 * Retrieve all properties from the entity type including public and non-public instance members.
		 * This enables schema generation for both public properties and internally managed properties.
		 */
		var properties = Dto.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
		var att = ResolveSchemaAttribute();

		if (string.IsNullOrWhiteSpace(att.Name))
			throw new NullReferenceException(att.Name);

		/*
		 * Initialize the entity schema with the resolved table name, schema name, and type.
		 * This forms the base structure for the schema definition.
		 */
		var result = new EntitySchema
		{
			Name = att.Name,
			Schema = att.Schema ?? SchemaAttribute.DefaultSchema,
			Type = SchemaAttribute.SchemaTypeTable
		};

		var columns = new List<SchemaColumn>();

		/*
		 * Iterate through all properties to build column definitions. Each property is analyzed
		 * for its attributes to determine how it should be represented as a database column.
		 */
		foreach (var property in properties)
		{
			/*
			 * Skip read-only properties as they cannot be persisted to storage.
			 */
			if (!property.CanWrite)
				continue;

			/*
			 * Skip virtual properties marked by PersistenceAttribute as they are computed
			 * or navigation properties not stored directly in the table.
			 */
			if (property.FindAttribute<PersistenceAttribute>() is PersistenceAttribute pa && pa.IsVirtual)
				continue;

			/*
			 * Create a new column definition with the resolved name and inferred data type.
			 */
			var column = new SchemaColumn(result, property)
			{
				Name = ResolveColumnName(property),
				DataType = property.PropertyType.ToDbType()
			};

			/*
			 * Override the inferred data type if explicitly specified via DataTypeAttribute.
			 */
			var dataType = property.FindAttribute<DataTypeAttribute>();

			if (dataType is not null)
				column.DataType = dataType.Type;

			/*
			 * Process primary key attributes to set identity, uniqueness, and index properties.
			 */
			var pk = property.FindAttribute<PrimaryKeyAttribute>();

			if (pk is not null)
			{
				column.IsPrimaryKey = true;
				column.IsIdentity = pk.IsIdentity;
				column.IsUnique = true;
				column.IsIndex = true;
			}

			/*
			 * Process index attributes to configure indexing and uniqueness constraints.
			 */
			var idx = property.FindAttribute<IndexAttribute>();

			if (idx is not null)
			{
				column.IsIndex = true;
				column.IsUnique = idx.Unique;
				column.Index = idx.Name;
			}

			/*
			 * Set the column ordinal for explicit column ordering if specified.
			 */
			var ordinal = property.FindAttribute<OrdinalAttribute>();

			if (ordinal is not null)
				column.Ordinal = ordinal.Ordinal;

			/*
			 * Configure precision and scale for decimal and numeric data types.
			 * If not specified, default to precision of 20 and scale of 5.
			 */
			if (column.DataType == DbType.Decimal
				 || column.DataType == DbType.VarNumeric)
			{
				var numeric = property.FindAttribute<NumericAttribute>();

				if (numeric is not null)
				{
					column.Precision = numeric.Precision;
					column.Scale = numeric.Scale;
				}
				else
				{
					column.Precision = 20;
					column.Scale = 5;
				}

			}
			/*
			 * Configure date and time columns with appropriate data types and precision settings.
			 * The DateAttribute allows explicit control over which date/time type is used.
			 */
			else if (column.DataType == DbType.Date
				 || column.DataType == DbType.DateTime
				 || column.DataType == DbType.DateTime2
				 || column.DataType == DbType.DateTimeOffset
				 || column.DataType == DbType.Time)
			{
				var dateAtt = property.FindAttribute<DateAttribute>();

				if (dateAtt is not null)
				{
					column.DateKind = dateAtt.Kind;
					column.DatePrecision = dateAtt.Precision;

					/*
					 * Map the DateKind enumeration to the appropriate DbType for storage.
					 */
					switch (dateAtt.Kind)
					{
						case DateKind.Date:
							column.DataType = DbType.Date;
							break;
						case DateKind.DateTime:
							column.DataType = DbType.DateTime;
							break;
						case DateKind.DateTime2:
							column.DataType = DbType.DateTime2;
							break;
						case DateKind.SmallDateTime:
							column.DataType = DbType.DateTime;
							break;
						case DateKind.Time:
							column.DataType = DbType.Time;
							break;
						case DateKind.Offset:
							column.DataType = DbType.DateTimeOffset;
							break;
					}
				}
				else
				{
					/*
					 * Default to DateTime2 with maximum precision (7) when no explicit date attribute is specified.
					 */
					column.DateKind = DateKind.DateTime2;
					column.DatePrecision = 7;
				}
			}
			/*
			 * Configure binary columns with the appropriate binary storage kind.
			 */
			else if (column.DataType == DbType.Binary)
			{
				var bin = property.FindAttribute<BinaryAttribute>();

				if (bin is not null)
					column.BinaryKind = bin.Kind;
			}
			/*
			 * Set default maximum length for string columns if not explicitly specified.
			 */
			else if (column.DataType == DbType.String
				 || column.DataType == DbType.AnsiString
				 || column.DataType == DbType.AnsiStringFixedLength
				 || column.DataType == DbType.StringFixedLength)
			{
				column.MaxLength = 50;
			}

			/*
			 * Parse and set the default value expression from the DefaultAttribute.
			 */
			ParseDefaultValue(column, property);

			/*
			 * Process ETag (version) columns for optimistic concurrency control.
			 * Version columns are binary with automatic update behavior.
			 */
			if (property.FindAttribute<ETagAttribute>() is not null)
			{
				column.IsVersion = true;
				column.DataType = DbType.Binary;
				column.MaxLength = 0;
			}
			else
			{
				/*
				 * Configure maximum length from LengthAttribute if specified.
				 */
				var maxLength = property.FindAttribute<LengthAttribute>();

				if (maxLength is not null)
					column.MaxLength = maxLength.Value;

				/*
				 * Determine nullability from NullableAttribute or infer from property type.
				 */
				var nullable = property.FindAttribute<NullableAttribute>();

				if (nullable is null)
					column.IsNullable = property.IsNullable();
				else
					column.IsNullable = nullable.IsNullable;
			}

			/*
			 * Remove maximum length specification for data types that don't support length constraints.
			 */
			if (column.MaxLength > 0 && !IsLengthSupported(column.DataType))
				column.MaxLength = 0;

			columns.Add(column);
		}

		/*
		 * Add columns to the schema ordered by explicit ordinal first, then alphabetically by name.
		 */
		if (columns.Count != 0)
			result.Columns.AddRange(columns.OrderBy(f => f.Ordinal).ThenBy(f => f.Name));

		return Task.FromResult<ISchema?>(result);
	}

	/// <summary>
	/// Resolves the table schema attribute from the entity type.
	/// </summary>
	/// <returns>The table attribute with resolved name and schema.</returns>
	/// <remarks>
	/// If no TableAttribute is present, creates a default one. The table name defaults to the
	/// entity type name converted to camel case, and the schema defaults to the standard schema name.
	/// </remarks>
	private TableAttribute ResolveSchemaAttribute()
	{
		var att = Dto.Type.GetCustomAttribute<TableAttribute>() ?? new TableAttribute();

		if (string.IsNullOrWhiteSpace(att.Name))
			att.Name = Dto.Type.Name.ToCamelCase();

		if (string.IsNullOrEmpty(att.Schema))
			att.Schema = TableAttribute.DefaultSchema;

		return att;
	}

	/// <summary>
	/// Resolves the column name for a property.
	/// </summary>
	/// <param name="property">The property to resolve the column name for.</param>
	/// <returns>The resolved column name.</returns>
	/// <remarks>
	/// If a MemberAttribute is present with a specified member name, that name is used.
	/// Otherwise, the property name is converted to camel case for the column name.
	/// </remarks>
	private static string ResolveColumnName(PropertyInfo property)
	{
		if (property.FindAttribute<MemberAttribute>() is not MemberAttribute mapping || string.IsNullOrWhiteSpace(mapping.Member))
			return property.Name.ToCamelCase();

		return mapping.Member;
	}

	/// <summary>
	/// Parses and sets the default value for a column from the property's DefaultAttribute.
	/// </summary>
	/// <param name="column">The column to set the default value for.</param>
	/// <param name="property">The property containing the default value attribute.</param>
	/// <remarks>
	/// Handles enum values by converting them to their underlying type before setting as the default.
	/// The default value is stored as an invariant culture string representation.
	/// </remarks>
	private static void ParseDefaultValue(SchemaColumn column, PropertyInfo property)
	{
		if (property.FindAttribute<DefaultAttribute>() is not DefaultAttribute def)
			return;

		var value = def.Value;

		/*
		 * Convert enum values to their underlying numeric type for proper storage.
		 */
		if (def.Value is not null && def.Value.GetType().IsEnum)
			value = Types.Convert(def.Value, def.Value.GetType().GetEnumUnderlyingType());

		column.DefaultValue = Convert.ToString(value, CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Determines whether a data type supports length specifications.
	/// </summary>
	/// <param name="type">The data type to check.</param>
	/// <returns><c>true</c> if the type supports length specifications; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// Only string and binary data types support length constraints. Numeric and temporal types do not.
	/// </remarks>
	private static bool IsLengthSupported(DbType type)
	{
		return type == DbType.AnsiString
			|| type == DbType.AnsiStringFixedLength
			|| type == DbType.String
			|| type == DbType.StringFixedLength
			|| type == DbType.Binary;
	}
}