using Connected.Annotations.Entities;
using Connected.Entities;
using Connected.Reflection;
using System.Data;
using System.Reflection;
using System.Text;

namespace Connected.Storage.PostgreSql.Transactions;

/// <summary>
/// Base class for building PostgreSQL database command text and parameters from entity operations.
/// </summary>
/// <remarks>
/// This abstract class provides the foundation for generating INSERT, UPDATE, and DELETE commands
/// for PostgreSQL databases. It handles property discovery, parameter creation, type mapping, and
/// command text generation using PostgreSQL-specific syntax including double-quoted identifiers.
/// The class uses a caching mechanism to optimize repeated operations on the same entity types.
/// Derived classes implement specific command generation logic for their respective operations
/// while inheriting common functionality for property handling and parameter management.
/// </remarks>
internal abstract class CommandBuilder
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CommandBuilder"/> class.
	/// </summary>
	protected CommandBuilder()
	{
		Parameters = [];
		WhereProperties = [];
		Properties = [];

		Text = new StringBuilder();
	}

	private StringBuilder Text { get; set; }

	/// <summary>
	/// Gets the list of entity properties to include in the command.
	/// </summary>
	protected List<PropertyInfo> Properties { get; }

	/// <summary>
	/// Gets the list of properties to use in WHERE clause conditions.
	/// </summary>
	protected List<PropertyInfo> WhereProperties { get; }

	/// <summary>
	/// Gets the generated command text.
	/// </summary>
	protected string CommandText => Text.ToString();

	/// <summary>
	/// Gets the entity being processed.
	/// </summary>
	protected IEntity? Entity { get; private set; }

	/// <summary>
	/// Gets the schema attribute from the entity.
	/// </summary>
	protected SchemaAttribute? Schema { get; private set; }

	/// <summary>
	/// Gets the list of parameters for the command.
	/// </summary>
	protected List<IStorageParameter> Parameters { get; }

	/// <summary>
	/// Builds a PostgreSQL storage operation for the specified entity.
	/// </summary>
	/// <param name="entity">The entity to build the command for.</param>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>A <see cref="PostgreSqlStorageOperation"/> containing the command text and parameters, or null if no operation is needed.</returns>
	public async Task<PostgreSqlStorageOperation?> Build(IEntity entity, CancellationToken cancel)
	{
		Entity = entity;
		DiscoverProperties();

		if (TryGetExisting(out PostgreSqlStorageOperation? existing))
		{
			if (existing is null)
				return null;

			/*
			 * We need to rebuild an instance since StorageOperation
			 * is immutable
			 */
			var result = new PostgreSqlStorageOperation
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
					result.Parameters.Add(new PostgreSqlStorageParameter
					{
						Value = parameter.Direction switch
						{
							ParameterDirection.Input => await GetValue(property, cancel),
							ParameterDirection.InputOutput => await GetValue(property, cancel),
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

	/// <summary>
	/// Builds the command for the entity when not found in cache.
	/// </summary>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>A <see cref="PostgreSqlStorageOperation"/> containing the command text and parameters.</returns>
	protected abstract Task<PostgreSqlStorageOperation> OnBuild(CancellationToken cancel);

	/// <summary>
	/// Tries to get an existing cached command for the entity type.
	/// </summary>
	/// <param name="result">The cached command operation if found.</param>
	/// <returns><c>true</c> if a cached command exists; otherwise, <c>false</c>.</returns>
	protected abstract bool TryGetExisting(out PostgreSqlStorageOperation? result);

	/// <summary>
	/// Writes text to the command builder.
	/// </summary>
	/// <param name="text">The text to write.</param>
	protected void Write(string text)
	{
		Text.Append(text);
	}

	/// <summary>
	/// Writes a single character to the command builder.
	/// </summary>
	/// <param name="text">The character to write.</param>
	protected void Write(char text)
	{
		Text.Append(text);
	}

	/// <summary>
	/// Writes a line of text to the command builder.
	/// </summary>
	/// <param name="text">The text to write followed by a line break.</param>
	protected void WriteLine(string text)
	{
		Text.AppendLine(text);
	}

	/// <summary>
	/// Trims trailing commas, whitespace, and line breaks from the command text.
	/// </summary>
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

	/// <summary>
	/// Discovers and filters entity properties for command generation.
	/// </summary>
	/// <remarks>
	/// This method identifies all properties that should be included in the command,
	/// excluding those marked with InMemory persistence mode.
	/// </remarks>
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

	/// <summary>
	/// Determines whether a property is marked as a version/ETag field.
	/// </summary>
	/// <param name="property">The property to check.</param>
	/// <returns><c>true</c> if the property has an ETagAttribute; otherwise, <c>false</c>.</returns>
	protected static bool IsVersion(PropertyInfo property)
	{
		return property.GetCustomAttribute<ETagAttribute>() is not null;
	}

	/// <summary>
	/// Gets the database column name for a property.
	/// </summary>
	/// <param name="property">The property to get the column name for.</param>
	/// <returns>The column name, either from MemberAttribute or the property name in camelCase.</returns>
	protected static string ColumnName(PropertyInfo property)
	{
		var dataMember = property.FindAttribute<MemberAttribute>();

		return dataMember is null || string.IsNullOrEmpty(dataMember.Member) ? property.Name.ToCamelCase() : dataMember.Member;
	}

	/// <summary>
	/// Resolves the database type for a property.
	/// </summary>
	/// <param name="property">The property to resolve the type for.</param>
	/// <returns>The <see cref="DbType"/> for the property.</returns>
	protected static DbType ResolveDbType(PropertyInfo property)
	{
		if (IsVersion(property))
			return DbType.Binary;

		var attribute = property.FindAttribute<DataTypeAttribute>();

		if (attribute is not null)
			return attribute.Type;

		return property.PropertyType.ToDbType();
	}

	/// <summary>
	/// Gets the value of a property from the entity.
	/// </summary>
	/// <param name="property">The property to get the value from.</param>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>The property value, or null if the property is null or default.</returns>
	protected Task<object?> GetValue(PropertyInfo property, CancellationToken cancel)
	{
		if (Entity is null)
			throw new NullReferenceException(Strings.ErrEntityExpected);

		if (IsNull(property))
			return Task.FromResult<object?>(null);

		if (IsVersion(property))
			return Task.FromResult<object?>((byte[])EntityVersion.Parse(property.GetValue(Entity)));

		return GetValue(Entity, property, property.GetValue(Entity), cancel);
	}

	/// <summary>
	/// Gets the value of a property using serialization if available.
	/// </summary>
	/// <param name="entity">The entity containing the property.</param>
	/// <param name="property">The property to get the value from.</param>
	/// <param name="value">The current value of the property.</param>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>The serialized property value, or the original value if no serializer is configured.</returns>
	private static Task<object?> GetValue(IEntity entity, PropertyInfo property, object? value, CancellationToken cancel)
	{
		var serializer = property.ResolveEntityPropertySerializer();

		if (serializer is null)
			return Task.FromResult(value);

		return serializer.Serialize(entity, value, cancel);
	}

	/// <summary>
	/// Determines whether a property value is null or default.
	/// </summary>
	/// <param name="property">The property to check.</param>
	/// <returns><c>true</c> if the property value is null or equals the default value for its type; otherwise, <c>false</c>.</returns>
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

	/// <summary>
	/// Creates a storage parameter for a property with Input direction.
	/// </summary>
	/// <param name="property">The property to create the parameter for.</param>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>A <see cref="PostgreSqlStorageParameter"/> for the property.</returns>
	protected Task<PostgreSqlStorageParameter> CreateParameter(PropertyInfo property, CancellationToken cancel)
	{
		return CreateParameter(property, ParameterDirection.Input, cancel);
	}

	/// <summary>
	/// Creates a storage parameter for a property with the specified direction.
	/// </summary>
	/// <param name="property">The property to create the parameter for.</param>
	/// <param name="direction">The parameter direction.</param>
	/// <param name="cancel">Cancellation token for the operation.</param>
	/// <returns>A <see cref="PostgreSqlStorageParameter"/> for the property.</returns>
	protected async Task<PostgreSqlStorageParameter> CreateParameter(PropertyInfo property, ParameterDirection direction, CancellationToken cancel)
	{
		var columnName = ColumnName(property);
		var parameterName = $"@{columnName}";

		var parameter = new PostgreSqlStorageParameter
		{
			Direction = direction,
			Name = parameterName,
			Type = ResolveDbType(property),
			Value = await GetValue(property, cancel)
		};

		Parameters.Add(parameter);

		return parameter;
	}

	/// <summary>
	/// Resolves a property from a parameter name.
	/// </summary>
	/// <param name="parameterName">The parameter name (with @ prefix).</param>
	/// <returns>The matching <see cref="PropertyInfo"/>, or null if not found.</returns>
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

	/// <summary>
	/// Resolves the full type name of the entity for caching purposes.
	/// </summary>
	/// <returns>The full type name of the entity.</returns>
	/// <exception cref="NullReferenceException">Thrown when entity or its type name is null.</exception>
	protected string ResolveEntityTypeName()
	{
		if (Entity is null)
			throw new NullReferenceException(Strings.ErrEntityExpected);

		var typeName = Entity.GetType().FullName;

		return typeName is null ? throw new NullReferenceException($"{Strings.ErrCannotResolveTypeName} ('{Entity.GetType()}')") : typeName;
	}
}
