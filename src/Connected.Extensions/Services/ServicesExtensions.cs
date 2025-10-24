using Connected.Annotations;
using Connected.Entities;
using Connected.Reflection;
using Connected.Services.Middlewares;
using System.Reflection;

namespace Connected.Services;

public static class ServicesExtensions
{
	/// <summary>
	/// Converts the <see cref="IDto"/> object into entity and overwrites the provided properties.
	/// </summary>
	/// <remarks>
	/// All provided arguments are used when overwriting properties in the order they  are specified. This means
	/// the value from the last defined property is used when setting the entity's value.
	/// </remarks>
	/// <typeparam name="TEntity">The type of the entity to create.</typeparam>
	/// <param name="dto">The dto containing base property set.</param>
	/// <param name="state">The state modifier to which entity is set.</param>
	/// <param name="sources">An array of additional modifier objects providing modified values.</param>
	/// <returns>A new instance of the entity with modified values.</returns>
	public static TEntity AsEntity<TEntity>(this IDto dto, State state, params object[] sources)
		where TEntity : IEntity
	{
		if (typeof(TEntity).CreateInstance<TEntity>() is not TEntity instance)
			throw new NullReferenceException(typeof(TEntity).Name);

		return Merge(instance, dto, state, sources);
	}
	public static List<Type> GetImplementedDtos(this Type type)
	{
		return GetImplementedInterfaces<IDto>(type);
	}

	public static Type? ResolveImplementedEntity(this Type type)
	{
		var interfaces = GetImplementedInterfaces<IEntity>(type);

		if (interfaces.Count == 0)
			return null;

		return interfaces[0];
	}

	private static List<Type> GetImplementedInterfaces<TInterface>(Type type)
	{
		var interfaces = type.GetInterfaces();
		var allInterfaces = new List<Type>();
		var baseInterfaces = new List<Type>();

		foreach (var i in interfaces)
		{
			if (typeof(TInterface)?.FullName is not string fullName)
				continue;

			if (i.GetInterface(fullName) is null)
				continue;

			if (i == typeof(TInterface))
				continue;

			allInterfaces.Add(i);

			foreach (var baseInterface in i.GetInterfaces())
			{
				if (baseInterface == typeof(TInterface) || typeof(TInterface)?.FullName is not string baseFullName)
					continue;

				if (baseInterface.GetInterface(baseFullName) is not null)
					baseInterfaces.Add(baseInterface);
			}
		}

		return [.. allInterfaces.Except(baseInterfaces)];
	}

	public static bool IsDtoImplementation(this Type type)
	{
		if (typeof(IDto)?.FullName is not string fullName)
			return false;

		return !type.IsInterface && !type.IsAbstract && type.GetInterface(fullName) is not null;
	}

	public static List<Type> GetImplementedServices(this Type type)
	{
		var result = new List<Type>();
		var interfaces = type.GetInterfaces();

		foreach (var i in interfaces)
		{
			if (i.GetCustomAttribute<ServiceAttribute>() is not null)
				result.Add(i);
		}

		return result;
	}

	public static string? ResolveServiceUrl(this Type type)
	{
		var services = type.GetImplementedServices();

		if (!services.Any())
			return null;

		foreach (var service in services)
		{
			var att = service.GetCustomAttribute<ServiceUrlAttribute>();

			if (att is not null)
				return att.Url;
		}

		return null;
	}

	public static bool IsService(this Type type)
	{
		var interfaces = type.GetInterfaces();

		foreach (var i in interfaces)
		{
			if (i.GetCustomAttribute<ServiceAttribute>() is not null)
				return true;
		}

		return false;
	}

	public static bool IsServiceOperationMiddleware(this Type type)
	{
		var f = typeof(IServiceOperationMiddleware).FullName;

		return f is not null && type.GetInterface(f) is not null;
	}

	public static bool IsServiceOperation(this Type type)
	{
		var f = typeof(IServiceOperation<>).FullName;

		return f is not null && type.GetInterface(f) is not null;
	}

	public static bool IsServiceFunction(this Type type)
	{
		var f = typeof(IFunction<,>).FullName;

		return (f is not null && type.GetInterface(f) is not null);
	}

	public static TDto AsDto<TDto>(this IEntity entity) where TDto : IDto
	{
		var instance = Dto.Factory.Create<TDto>();
		var result = Serializer.Merge(instance, entity);

		return result is null ? throw new NullReferenceException(Strings.ErrMergeNull) : result;
	}

	public static TDto AsDto<TDto>(this IDto dto) where TDto : IDto
	{
		var instance = Dto.Factory.Create<TDto>();
		var result = Serializer.Merge(instance, dto);

		return result is null ? throw new NullReferenceException(Strings.ErrMergeNull) : result;
	}

	public static TDto AsDto<TDto, TPrimaryKey>(this IPrimaryKeyEntity<TPrimaryKey> entity)
		where TDto : IDto
		where TPrimaryKey : notnull
	{
		var instance = Dto.Factory.Create<TDto>();
		var result = Serializer.Merge(instance, entity);

		return result is null ? throw new NullReferenceException(Strings.ErrMergeNull) : result;
	}

	public static TDto Patch<TDto, TEntity>(this IDto dto, TEntity? entity, params object[] sources)
		where TDto : IDto
		where TEntity : IEntity
	{
		if (entity is null)
			return dto.AsEntity<TEntity>(State.Update).AsDto<TDto>(dto);

		return Merge(entity, dto, State.Update, sources).AsDto<TDto>();
	}

	public static TDto AsDto<TDto>(this IEntity entity, params object[] sources) where TDto : IDto
	{
		var instance = Dto.Factory.Create<TDto>();
		var result = Serializer.Merge(instance, entity, sources);

		return result is null ? throw new NullReferenceException(Strings.ErrMergeNull) : result;
	}

	public static TEntity Merge<TEntity>(this TEntity existing, IDto? modifier, State state, params object[] sources)
		where TEntity : IEntity
	{
		TEntity? newEntity;

		if (typeof(TEntity).IsInterface && existing.GetType().IsAssignableTo(typeof(TEntity)))
			newEntity = (TEntity)(Activator.CreateInstance(existing.GetType()) ?? throw new NullReferenceException());
		else
			newEntity = Activator.CreateInstance<TEntity>();

		return Serializer.Merge(newEntity, existing, modifier, new StateModifier { State = state }, sources);
	}
}
