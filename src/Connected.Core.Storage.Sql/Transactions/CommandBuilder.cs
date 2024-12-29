using Connected.Annotations.Entities;
using Connected.Entities;
using Connected.Reflection;
using System.Data;
using System.Reflection;
using System.Text;

namespace Connected.Storage.Sql.Transactions;

internal abstract class CommandBuilder
{
	protected CommandBuilder()
	{
		Parameters = new();
		WhereProperties = new();
		Properties = new();

		Text = new StringBuilder();
	}

	private StringBuilder Text { get; set; }
	protected List<PropertyInfo> Properties { get; }
	protected List<PropertyInfo> WhereProperties { get; }
	protected string CommandText => Text.ToString();
	protected IEntity? Entity { get; private set; }
	protected SchemaAttribute? Schema { get; private set; }
	protected List<IStorageParameter> Parameters { get; }

	public async Task<SqlStorageOperation?> Build(IEntity entity, CancellationToken cancel)
	{
		Entity = entity;
		DiscoverProperties();

		if (TryGetExisting(out SqlStorageOperation? existing))
		{
			if (existing is null)
				return null;
			/*
          * We need to rebuild an instance since StorageOperation
          * is immutable
          */
			var result = new SqlStorageOperation
			{
				CommandText = existing.CommandText,
				CommandTimeout = existing.CommandTimeout,
				CommandType = existing.CommandType,
				Concurrency = existing.Concurrency
			};

			if (result.Parameters is null)
				return result;

			foreach (var parameter in existing.Parameters)
			{
				if (parameter.Name is null)
					continue;

				if (ResolveProperty(parameter.Name) is PropertyInfo property)
				{
					result.Parameters.Add(new SqlStorageParameter
					{
						Value = parameter.Direction switch
						{
							ParameterDirection.Input => await GetValue(property, cancel),
							_ => default
						},
						Name = parameter.Name,
						Type = parameter.Type,
						Direction = parameter.Direction
					});
				}
			}

			return result;
		}

		Schema = Entity.GetSchemaAttribute();

		return await OnBuild(cancel);
	}


	protected abstract Task<SqlStorageOperation> OnBuild(CancellationToken cancel);

	protected abstract bool TryGetExisting(out SqlStorageOperation? result);

	protected void Write(string text)
	{
		Text.Append(text);
	}

	protected void Write(char text)
	{
		Text.Append(text);
	}

	protected void WriteLine(string text)
	{
		Text.AppendLine(text);
	}

	protected void Trim()
	{
		for (var i = Text.Length - 1; i >= 0; i--)
		{
			if (!Text[i].Equals(',') && !Text[i].Equals('\n') && !Text[i].Equals('\r') && !Text[i].Equals(' '))
				break;

			if (i < Text.Length)
				Text.Length = i;
		}
	}

	protected virtual void DiscoverProperties()
	{
		if (Entity is null)
			throw new NullReferenceException(Strings.ErrEntityExpected);

		var props = Reflection.Properties.GetImplementedProperties(Entity);

		foreach (var property in props)
		{
			var persistence = property.FindAttribute<PersistenceAttribute>();

			if (persistence is not null && persistence.Mode.HasFlag(PersistenceMode.InMemory))
				continue;

			Properties.Add(property);
		}
	}

	protected static bool IsVersion(PropertyInfo property)
	{
		return property.GetCustomAttribute<ETagAttribute>() is not null;
	}

	protected static string ColumnName(PropertyInfo property)
	{
		var dataMember = property.FindAttribute<MemberAttribute>();

		return dataMember is null || string.IsNullOrEmpty(dataMember.Member) ? property.Name.ToCamelCase() : dataMember.Member;
	}

	protected static DbType ResolveDbType(PropertyInfo property)
	{
		if (IsVersion(property))
			return DbType.Binary;

		var attribute = property.FindAttribute<DataTypeAttribute>();

		if (attribute is not null)
			return attribute.Type;

		return property.PropertyType.ToDbType();
	}

	protected Task<object?> GetValue(PropertyInfo property, CancellationToken cancel)
	{
		if (Entity is null)
			throw new NullReferenceException(Strings.ErrEntityExpected);

		if (IsNull(property))
			return Task.FromResult<object?>(null);

		if (IsVersion(property))
			return Task.FromResult<object?>((byte[])EntityVersion.Parse(property.GetValue(Entity)));

		return GetValue(Entity, property, property.GetValue(Entity), property.PropertyType.ToDbType(), cancel);
	}

	private static Task<object?> GetValue(IEntity entity, PropertyInfo property, object? value, DbType dbType, CancellationToken cancel)
	{
		var serializer = property.ResolveEntityPropertySerializer();

		if (serializer is null)
			return Task.FromResult(value);

		return serializer.Serialize(entity, value, cancel);
	}

	private bool IsNull(PropertyInfo property)
	{
		var result = property.GetValue(Entity);

		if (result is null)
			return true;

		if (property.GetCustomAttribute<NullableAttribute>() is null)
			return false;

		var def = Types.GetDefault(property.PropertyType);

		return TypeComparer.Compare(result, def);
	}

	protected Task<SqlStorageParameter> CreateParameter(PropertyInfo property, CancellationToken cancel)
	{
		return CreateParameter(property, ParameterDirection.Input, cancel);
	}

	protected async Task<SqlStorageParameter> CreateParameter(PropertyInfo property, ParameterDirection direction, CancellationToken cancel)
	{
		var columnName = ColumnName(property);
		var parameterName = $"@{columnName}";

		var parameter = new SqlStorageParameter
		{
			Direction = direction,
			Name = parameterName,
			Type = ResolveDbType(property),
			Value = await GetValue(property, cancel)
		};

		Parameters.Add(parameter);

		return parameter;
	}

	private PropertyInfo? ResolveProperty(string parameterName)
	{
		if (Entity is null)
			throw new NullReferenceException(Strings.ErrEntityExpected);

		var propertyName = parameterName[1..];
		var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

		if (Entity.GetType().GetProperty(propertyName.ToPascalCase(), flags) is PropertyInfo property)
			return property;

		if (Entity.GetType().GetProperty(propertyName, flags) is PropertyInfo raw)
			return raw;

		return null;
	}

	protected string ResolveEntityTypeName()
	{
		if (Entity is null)
			throw new NullReferenceException(Strings.ErrEntityExpected);

		var typeName = Entity.GetType().FullName;

		if (typeName is null)
			throw new NullReferenceException($"{Strings.ErrCannotResolveTypeName} ('{Entity.GetType()}')");

		return typeName;
	}
}
