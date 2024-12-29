using Connected.Annotations.Entities;
using Connected.Reflection;
using Connected.Services;
using Connected.Storage;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace Connected.Entities;

public static class EntitiesExtensions
{
	public static TResultEntity Merge<TEntity, TResultEntity>(this TEntity existing, params object[] sources)
		where TEntity : IEntity
		where TResultEntity : IEntity
	{
		var newEntity = Activator.CreateInstance<TResultEntity>();

		return Serializer.Merge(newEntity, existing, sources);
	}

	public static TEntity Clone<TEntity>(this TEntity existing)
		where TEntity : IEntity
	{
		var newEntity = Activator.CreateInstance<TEntity>();

		return Serializer.Merge(newEntity, existing);
	}

	public static async Task<ImmutableList<TSource>> AsEntities<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			return [];

		var list = new List<TSource>();

		await foreach (var element in source.AsAsyncEnumerable().WithCancellation(cancellationToken))
			list.Add(element);

		return [.. list];
	}

	/*
	 * This method is actually not asynchronous but to avoid future refactorings its signature is 
	 * marked as async.
	 */
	public static async Task<ImmutableList<TSource>> AsEntities<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		if (source is null)
			return [];

		await Task.CompletedTask;

		return source.Where(predicate).ToImmutableList();
	}

	public static async Task<ImmutableList<TSource>> AsEntities<TSource>(this IEnumerable<TSource> source)
	{
		if (source is null)
			return [];

		await Task.CompletedTask;

		return source.ToImmutableList();
	}

	public static Task<ImmutableList<TDestination>> AsEntities<TSource, TDestination>(this IEnumerable<TSource> source)
		where TSource : TDestination
	{
		if (source is null)
			return Task.FromResult(ImmutableList<TDestination>.Empty);

		return Task.FromResult(source.Cast<TDestination>().ToImmutableList());
	}

	public static async Task<TSource?> AsEntity<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			return default;

		return await Execute<TSource, TSource>(QueryableMethods.SingleOrDefaultWithoutPredicate, source, cancellationToken);
	}

	public static Task<TSource?> AsEntity<TSource>(this IEnumerable<TSource> source)
	{
		if (source is null)
			return Task.FromResult<TSource?>(default);

		return Task.FromResult(source.FirstOrDefault());
	}

	public static Task<TSource?> AsEntity<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		if (source is null)
			return Task.FromResult<TSource?>(default);

		return Task.FromResult(source.FirstOrDefault(predicate));
	}

	public static IEnumerable<TEntity> WithDto<TEntity>(this IEnumerable<TEntity> source, IQueryDto dto)
	{
		return source.WithOrderBy(dto).WithPaging(dto);
	}

	private static IEnumerable<TEntity> WithPaging<TEntity>(this IEnumerable<TEntity> source, IQueryDto dto)
	{
		if (dto.Paging.Size < 1)
			return source;

		return source.Skip((dto.Paging.Index - 1) * dto.Paging.Size)
				.Take(dto.Paging.Size);
	}

	private static IEnumerable<TEntity> WithOrderBy<TEntity>(this IEnumerable<TEntity> entities, IQueryDto dto)
	{
		if (entities.AsQueryable() as IOrderedQueryable<TEntity> is not IOrderedQueryable<TEntity> result)
			return entities;

		var top = true;

		foreach (var criteria in dto.OrderBy)
		{
			if (top)
			{
				if (criteria.Mode == OrderByMode.Ascending)
					result = result.OrderBy(ResolvePropertyPredicate<TEntity>(criteria.Property));
				else
					result = result.OrderByDescending(ResolvePropertyPredicate<TEntity>(criteria.Property));
			}
			else
			{
				if (criteria.Mode == OrderByMode.Ascending)
					result = result.ThenBy(ResolvePropertyPredicate<TEntity>(criteria.Property));
				else
					result = result.ThenByDescending(ResolvePropertyPredicate<TEntity>(criteria.Property));
			}

			top = false;
		}

		return result;
	}

	private static Expression<Func<T, object>> ResolvePropertyPredicate<T>(string propToOrder)
	{
		var param = Expression.Parameter(typeof(T));
		var memberAccess = Expression.Property(param, propToOrder);
		var convertedMemberAccess = Expression.Convert(memberAccess, typeof(object));
		var orderPredicate = Expression.Lambda<Func<T, object>>(convertedMemberAccess, param);

		return orderPredicate;
	}

	private static async Task<TResult?> Execute<TSource, TResult>(MethodInfo operatorMethodInfo, IQueryable<TSource> source, Expression? expression)
	{
		if (operatorMethodInfo.IsGenericMethod)
		{
			operatorMethodInfo = operatorMethodInfo.GetGenericArguments().Length == 2
					  ? operatorMethodInfo.MakeGenericMethod(typeof(TSource), typeof(TResult).GetGenericArguments().Single())
					  : operatorMethodInfo.MakeGenericMethod(typeof(TSource));
		}

		await Task.CompletedTask;

		try
		{
			var arguments = expression is null ? [source.Expression] : new[] { source.Expression, expression };
			var callExpression = Expression.Call(instance: null, method: operatorMethodInfo, arguments: arguments);
			var result = source.Provider.Execute(callExpression);

			if (result is null)
				return default;

			return (TResult)result;
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}

	private static async Task<TResult?> Execute<TSource, TResult>(MethodInfo operatorMethodInfo, IQueryable<TSource> source, CancellationToken cancellationToken = default)
	{
		return await Execute<TSource, TResult>(operatorMethodInfo, source, null);
	}

	public static PropertyInfo? PrimaryKeyProperty(this IEntity entity)
	{
		foreach (var property in entity.GetType().GetInheritedProperites())
		{
			if (property.FindAttribute<PrimaryKeyAttribute>() is not null)
				return property;
		}

		return null;
	}

	public static object? PrimaryKeyValue(this IEntity entity)
	{
		if (PrimaryKeyProperty(entity) is not PropertyInfo property)
			return null;

		return property.GetValue(entity);
	}
	public static string EntityId(this IEntity entity)
	{
		var attribute = entity.GetSchemaAttribute();

		return $"{attribute.Schema}.{attribute.Name}";
	}
	public static SchemaAttribute GetSchemaAttribute(this IEntity entity)
	{
		var definedAttribute = entity.GetType().GetCustomAttribute<SchemaAttribute>();

		if (definedAttribute is not null && definedAttribute.Schema is not null && definedAttribute.Name is not null)
			return definedAttribute;

		var schema = string.IsNullOrEmpty(definedAttribute?.Schema) ? SchemaAttribute.DefaultSchema : definedAttribute.Schema;
		var name = string.IsNullOrEmpty(definedAttribute?.Name) ? entity.GetType().Name.ToPascalCase() : definedAttribute.Name;

		return new TableAttribute
		{
			Id = definedAttribute?.Id,
			Name = name,
			Schema = schema
		};
	}

	public static IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(this IQueryable<TSource> source)
	{
		if (source is IAsyncEnumerable<TSource> asyncEnumerable)
			return asyncEnumerable;

		throw new InvalidOperationException();
	}

	public static IStorageParameter? ResolveParameter(this IStorageOperation operation, string criteria)
	{
		var exact = operation.Parameters.FirstOrDefault(f => string.Equals(f.Name, criteria, StringComparison.OrdinalIgnoreCase));

		if (exact is not null)
			return exact;

		foreach (var parameter in operation.Parameters)
		{
			if (parameter.Name is null)
				continue;

			if (parameter.Name.StartsWith("@"))
			{
				var guess = parameter.Name[1..];

				if (string.Equals(guess, criteria, StringComparison.OrdinalIgnoreCase))
					return parameter;
			}
		}

		return null;
	}

	public static IStorageVariable? ResolveVariable(this IStorageOperation operation, string criteria)
	{
		return operation.Variables.FirstOrDefault(f => string.Equals(f.Name, criteria, StringComparison.OrdinalIgnoreCase));
	}

	public static T? ResolveParameterValue<T>(this IStorageOperation operation, string criteria)
	{
		var parameter = operation.ResolveParameter(criteria);

		if (parameter is null || parameter.Value is null)
			return default;

		return (T?)Convert.ChangeType(parameter.Value, typeof(T));
	}

	public static T? ResolveVariableValue<T>(this IStorageOperation operation, string criteria)
	{
		var variable = operation.ResolveVariable(criteria);

		if (variable is null || !variable.Values.Any())
			return default;

		var firstNonNull = variable.Values.FirstOrDefault(f => f is not null);

		if (firstNonNull is null)
			return default;

		return (T?)Convert.ChangeType(firstNonNull, typeof(T));
	}

	public static string EntityKey(this Type type)
	{
		if (!type.ImplementsInterface<IEntity>())
			return type.RawEntityKey();

		var attribute = type.GetCustomAttribute<TableAttribute>();

		if (attribute is null || attribute.Schema is null)
			return type.RawEntityKey();

		return $"{attribute.Schema}_{type.Name.ToCamelCase()}";
	}

	private static string RawEntityKey(this Type type)
	{
		var typeName = type.FullName;

		if (typeName is null)
			return type.Name.ToCamelCase();

		return typeName.ToCamelCase();
	}
	/// <summary>
	/// Returns the type of the entity that a passes type implements.
	/// </summary>
	/// <remarks>
	/// Type must implement the entity's interface directly. If the type
	/// inherits from another type it must explicitly specify the interface
	/// implementation otherwise this method won't resolve the entity.
	/// </remarks>
	public static Type? GetUnderlyingEntity(Type type)
	{
		var interfaces = type.BaseType is null
			? type.GetInterfaces()
			: type.GetInterfaces().Except(type.BaseType.GetInterfaces());
		var candidates = new List<Type>();

		foreach (var i in interfaces)
		{
			if (i == typeof(IEntity))
				continue;

			if (i.IsAssignableTo(typeof(IEntity)))
				return i;
		}

		return null;
	}

	public static IEntityPropertySerializer? ResolveEntityPropertySerializer(this PropertyInfo property)
	{
		var attribute = property.FindAttribute<SerializerAttribute>();
		IEntityPropertySerializer? serializer = null;

		if (attribute is null)
			return null;

		var serializerInstance = attribute.Type.CreateInstance();

		if (serializerInstance is null)
			throw new NullReferenceException($"{Strings.ErrCreateInstanceNull} ('{attribute.Type}')");

		serializer = serializerInstance as IEntityPropertySerializer;

		if (serializer is null)
			throw new NullReferenceException($"{Strings.ErrInterfaceExpected} ('{attribute.Type}, {nameof(IEntityPropertySerializer)}')");

		return serializer;
	}
}
