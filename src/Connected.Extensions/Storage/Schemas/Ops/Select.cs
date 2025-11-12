using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Reflection;
using Connected.Services;
using System.Data;
using System.Globalization;
using System.Reflection;

namespace Connected.Storage.Schemas.Ops;

internal sealed class Select : ServiceFunction<ISelectSchemaDto, ISchema?>
{
	protected override Task<ISchema?> OnInvoke()
	{
		var properties = Dto.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
		var att = ResolveSchemaAttribute();

		if (string.IsNullOrWhiteSpace(att.Name))
			throw new NullReferenceException(att.Name);

		var result = new EntitySchema
		{
			Name = att.Name,
			Schema = att.Schema ?? SchemaAttribute.DefaultSchema,
			Type = SchemaAttribute.SchemaTypeTable
		};

		var columns = new List<SchemaColumn>();

		foreach (var property in properties)
		{
			if (!property.CanWrite)
				continue;

			if (property.FindAttribute<PersistenceAttribute>() is PersistenceAttribute pa && pa.IsVirtual)
				continue;

			var column = new SchemaColumn(result, property)
			{
				Name = ResolveColumnName(property),
				DataType = property.PropertyType.ToDbType()
			};

			var dataType = property.FindAttribute<DataTypeAttribute>();

			if (dataType is not null)
				column.DataType = dataType.Type;

			var pk = property.FindAttribute<PrimaryKeyAttribute>();

			if (pk is not null)
			{
				column.IsPrimaryKey = true;
				column.IsIdentity = pk.IsIdentity;
				column.IsUnique = true;
				column.IsIndex = true;
			}

			var idx = property.FindAttribute<IndexAttribute>();

			if (idx is not null)
			{
				column.IsIndex = true;
				column.IsUnique = idx.Unique;
				column.Index = idx.Name;
			}

			var ordinal = property.FindAttribute<OrdinalAttribute>();

			if (ordinal is not null)
				column.Ordinal = ordinal.Ordinal;

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
					column.DateKind = DateKind.DateTime2;
					column.DatePrecision = 7;
				}
			}
			else if (column.DataType == DbType.Binary)
			{
				var bin = property.FindAttribute<BinaryAttribute>();

				if (bin is not null)
					column.BinaryKind = bin.Kind;
			}
			else if (column.DataType == DbType.String
				 || column.DataType == DbType.AnsiString
				 || column.DataType == DbType.AnsiStringFixedLength
				 || column.DataType == DbType.StringFixedLength)
			{
				column.MaxLength = 50;
			}

			ParseDefaultValue(column, property);

			if (property.FindAttribute<ETagAttribute>() is not null)
			{
				column.IsVersion = true;
				column.DataType = DbType.Binary;
				column.MaxLength = 0;
			}
			else
			{
				var maxLength = property.FindAttribute<LengthAttribute>();

				if (maxLength is not null)
					column.MaxLength = maxLength.Value;

				var nullable = property.FindAttribute<NullableAttribute>();

				if (nullable is null)
					column.IsNullable = property.IsNullable();
				else
					column.IsNullable = nullable.IsNullable;
			}

			if (column.MaxLength > 0 && !IsLengthSupported(column.DataType))
				column.MaxLength = 0;

			columns.Add(column);
		}

		if (columns.Count != 0)
			result.Columns.AddRange(columns.OrderBy(f => f.Ordinal).ThenBy(f => f.Name));

		return Task.FromResult<ISchema?>(result);
	}

	private TableAttribute ResolveSchemaAttribute()
	{
		var att = Dto.Type.GetCustomAttribute<TableAttribute>() ?? new TableAttribute();

		if (string.IsNullOrWhiteSpace(att.Name))
			att.Name = Dto.Type.Name.ToCamelCase();

		if (string.IsNullOrEmpty(att.Schema))
			att.Schema = TableAttribute.DefaultSchema;

		return att;
	}

	private static string ResolveColumnName(PropertyInfo property)
	{
		if (property.FindAttribute<MemberAttribute>() is not MemberAttribute mapping || string.IsNullOrWhiteSpace(mapping.Member))
			return property.Name.ToCamelCase();

		return mapping.Member;
	}

	private static void ParseDefaultValue(SchemaColumn column, PropertyInfo property)
	{
		if (property.FindAttribute<DefaultAttribute>() is not DefaultAttribute def)
			return;

		var value = def.Value;

		if (def.Value is not null && def.Value.GetType().IsEnum)
			value = Types.Convert(def.Value, def.Value.GetType().GetEnumUnderlyingType());

		column.DefaultValue = Convert.ToString(value, CultureInfo.InvariantCulture);
	}

	private static bool IsLengthSupported(DbType type)
	{
		return type == DbType.AnsiString
			|| type == DbType.AnsiStringFixedLength
			|| type == DbType.String
			|| type == DbType.StringFixedLength
			|| type == DbType.Binary;
	}
}