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

	public static TResultEntity WithState<TEntity, TResultEntity>(this TEntity existing, State state)
		where TEntity : IEntity
		where TResultEntity : IEntity
	{
		var newEntity = Activator.CreateInstance<TResultEntity>();

		return Serializer.Merge(newEntity, existing, new
		{
			State = state
		});
	}

	public static TEntity WithState<TEntity>(this TEntity existing, State state)
		where TEntity : IEntity
	{
		var newEntity = Activator.CreateInstance<TEntity>();

		return Serializer.Merge(newEntity, existing, new
		{
			State = state
		});
	}

	public static TEntity Clone<TEntity>(this TEntity existing)
		where TEntity : IEntity
	{
		var newEntity = Activator.CreateInstance<TEntity>();

		return Serializer.Merge(newEntity, existing);
	}

	private static async Task<IImmutableList<TSource>> Enumerate<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			return [];

		var list = new List<TSource>();

		await foreach (var element in source.AsAsyncEnumerable().WithCancellation(cancellationToken))
			list.Add(element);

		return [.. list];
	}

	public static async Task<IImmutableList<TSource>> AsEntities<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
	{
		return await Enumerate(source, cancellationToken);
	}

	public static async Task<IImmutableList<TSource>> AsEntities<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		if (source is null)
			return [];

		await Task.CompletedTask;

		if (predicate != null)
		{
			if (typeof(TSource).IsInterface && source.ElementType != typeof(TSource) && source.ElementType.IsAssignableTo(typeof(TSource)))
				return RewriteAndExecuteMulti(source, predicate);

			source = source.Where(predicate);
		}

		return await Enumerate(source);
	}


	public static async Task<TSource?> AsEntity<TSource>(this IQueryable<TSource> source)
	{
		var result = await Enumerate(source);

		if (result == null || result.Count == 0)
			return default;

		return result[0];
	}

	public static async Task<TSource?> AsEntity<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		if (source is null)
			return default;

		if (predicate is not null)
		{
			if (typeof(TSource).IsInterface && source.ElementType != typeof(TSource) && source.ElementType.IsAssignableTo(typeof(TSource)))
				return await RewriteAndExecuteSingle(source, predicate);

			source = source.Where(predicate);
		}

		var filtered = await Enumerate(source);

		if (filtered == null || filtered.Count == 0)
			return default;

		return filtered[0];
	}

	public static IQueryable<TEntity> WithDto<TEntity>(this IQueryable<TEntity> source, IQueryDto dto)
	{
		if (ResolveDynamicPredicate<TEntity>(dto) is Expression<Func<TEntity, bool>> predicate)
			source = source.Where(predicate);

		return source.WithOrderBy(dto).WithPaging(dto);
	}

	private static IQueryable<TEntity> WithPaging<TEntity>(this IQueryable<TEntity> source, IQueryDto dto)
	{
		if (dto.Paging.Size < 1)
			return source;

		return source.Skip(dto.Paging.Index * dto.Paging.Size).Take(dto.Paging.Size);
	}

	private static IQueryable<TEntity> WithOrderBy<TEntity>(this IQueryable<TEntity> entities, IQueryDto dto)
	{
		if (dto.OrderBy == null || dto.OrderBy.Count == 0)
			return entities;

		IOrderedQueryable<TEntity>? result = null;

		foreach (var criteria in dto.OrderBy)
		{
			var selector = ResolvePropertyPredicate<TEntity>(criteria.Property);

			if (result == null)
			{
				if (criteria.Mode == OrderByMode.Ascending)
					result = entities.OrderBy(selector);
				else
					result = entities.OrderByDescending(selector);
			}
			else
			{
				if (criteria.Mode == OrderByMode.Ascending)
					result = result.ThenBy(selector);
				else
					result = result.ThenByDescending(selector);
			}
		}

		return result ?? entities;
	}

	private static Expression<Func<TEntity, bool>>? ResolveDynamicPredicate<TEntity>(IQueryDto dto)
	{
		if (dto is IDynamicQueryDto<TEntity> dynamicDto && dynamicDto.Predicate is not null)
			return dynamicDto.Predicate;

		if (typeof(TEntity).IsInterface)
			return null;

		var entityType = typeof(TEntity).ResolveImplementedEntity();

		if (entityType == null)
			return null;

		var dynamicDtoDefinition = typeof(IDynamicQueryDto<>).MakeGenericType(entityType);

		if (dto.GetType().IsAssignableTo(dynamicDtoDefinition))
		{
			var predicate = dto.GetType().GetProperty(nameof(IDynamicQueryDto<object>.Predicate), BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);

			if (predicate == null)
				return null;

			return predicate.GetValue(dto) as Expression<Func<TEntity, bool>>;
		}

		return null;
	}

	private static Expression<Func<T, object>> ResolvePropertyPredicate<T>(string propToOrder)
	{
		var param = Expression.Parameter(typeof(T));
		var property = ResolveProperty(typeof(T), propToOrder)
			?? throw new NullReferenceException($"Property '{propToOrder}' is not defined for type '{typeof(T).FullName}'.");

		var memberAccess = Expression.Property(param, property);
		var convertedMemberAccess = Expression.Convert(memberAccess, typeof(object));
		var orderPredicate = Expression.Lambda<Func<T, object>>(convertedMemberAccess, param);

		return orderPredicate;
	}

	private static PropertyInfo? ResolveProperty(Type type, string propertyName)
	{
		var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

		if (property is not null)
			return property;

		foreach (var iface in type.GetInterfaces())
		{
			property = iface.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

			if (property is not null)
				return property;
		}

		return null;
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

	public static string EntityKey(this Type type)
	{
		var attribute = type.GetCustomAttribute<EntityKeyAttribute>() ?? throw new NullReferenceException($"{SR.ErrEntityKeyExpected} ({type.ShortName()})");

		return attribute.Key;
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

		if (source?.GetType().IsEnumerable() == true)
			return new AdHocAsyncEnumerable<TSource>(source);

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

			if (parameter.Name.StartsWith('@'))
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

		return Types.Convert<T>(parameter.Value);
	}

	public static T? ResolveVariableValue<T>(this IStorageOperation operation, string criteria)
	{
		var variable = operation.ResolveVariable(criteria);

		if (variable is null || variable.Values.Count == 0)
			return default;

		var firstNonNull = variable.Values.FirstOrDefault(f => f is not null);

		if (firstNonNull is null)
			return default;

		return Types.Convert<T>(firstNonNull);
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

		var serializerInstance = attribute.Type.CreateInstance() ?? throw new NullReferenceException($"{Strings.ErrCreateInstanceNull} ('{attribute.Type}')");

		serializer = serializerInstance as IEntityPropertySerializer ?? throw new NullReferenceException($"{Strings.ErrInterfaceExpected} ('{attribute.Type}, {nameof(IEntityPropertySerializer)}')");

		return serializer;
	}

	public static string DistributedKey<THead, TPrimaryKey>(this IDistributedEntity<THead, TPrimaryKey> entity)
		where THead : notnull
		where TPrimaryKey : notnull
	{
		return $"{entity.Head}.{entity.Id}";
	}

	public static TEntity Required<TEntity>(this TEntity? entity)
		where TEntity : IEntity
	{
		if (entity is null)
			throw new NullReferenceException($"{Strings.ErrEntityExpected} ({typeof(TEntity).ShortName()})");

		return entity;
	}

	public static TEntity Required<TEntity>(this IEntity? entity)
		where TEntity : IEntity
	{
		if (entity is null)
			throw new NullReferenceException($"{Strings.ErrEntityExpected} ({typeof(TEntity).ShortName()})");

		return (TEntity)entity;
	}

	private static Task<TSource?> RewriteAndExecuteSingle<TSource>(IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		foreach (var item in BuildRewrittenQuery(source, predicate))
			return Task.FromResult<TSource?>((TSource)item!);

		return Task.FromResult<TSource?>(default);
	}

	private static IImmutableList<TSource> RewriteAndExecuteMulti<TSource>(IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		var list = new List<TSource>();

		foreach (var item in BuildRewrittenQuery(source, predicate))
			list.Add((TSource)item!);

		return [.. list];
	}

	private static IQueryable BuildRewrittenQuery<TSource>(IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
	{
		var elementType = source.ElementType;
		var originalParam = predicate.Parameters[0];
		var newParam = Expression.Parameter(elementType, originalParam.Name);
		var newBody = new ParameterTypeRewriter(originalParam, newParam).Visit(predicate.Body)!;
		var newLambda = Expression.Lambda(newBody, newParam);
		var whereMethod = GetQueryableWhereMethod(elementType);
		var callExpr = Expression.Call(null, whereMethod, source.Expression, Expression.Quote(newLambda));

		return source.Provider.CreateQuery(callExpr);
	}

	private static MethodInfo GetQueryableWhereMethod(Type elementType)
	{
		var method = typeof(Queryable)
			.GetMethods(BindingFlags.Static | BindingFlags.Public)
			.First(m =>
			{
				if (m.Name != nameof(Queryable.Where))
					return false;

				var parameters = m.GetParameters();

				if (parameters.Length != 2)
					return false;

				var paramType = parameters[1].ParameterType;

				if (!paramType.IsGenericType)
					return false;

				var funcType = paramType.GetGenericArguments().FirstOrDefault();

				return funcType?.IsGenericType == true && funcType.GetGenericArguments().Length == 2;
			});

		return method.MakeGenericMethod(elementType);
	}

	private sealed class ParameterTypeRewriter(ParameterExpression oldParam, ParameterExpression newParam) : ExpressionVisitor
	{
		protected override Expression VisitParameter(ParameterExpression node)
			=> node == oldParam ? newParam : base.VisitParameter(node);

		protected override Expression VisitMember(MemberExpression node)
		{
			if (node.Expression is ParameterExpression pe && pe == oldParam)
			{
				var member = newParam.Type
					.GetMember(node.Member.Name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance)
					.FirstOrDefault();

				return Expression.MakeMemberAccess(newParam, member ?? node.Member);
			}

			return base.VisitMember(node);
		}
	}
}
