using Connected.Authorization;
using Connected.Entities;
using Connected.Reflection;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Connected.Services;

internal sealed class OperationResultAuthorizer<TDto, TResult>
	where TDto : IDto
{
	static OperationResultAuthorizer()
	{
		EntityMap = new();
	}

	public OperationResultAuthorizer(IAuthorizationContext authorization)
	{
		Authorization = authorization;
	}

	private static ConcurrentDictionary<Type, bool> EntityMap { get; }
	private IAuthorizationContext Authorization { get; }

	public async Task<TResult> Authorize(ICallerContext caller, TDto dto, TResult result)
	{
		if (result is null)
			throw new NullReferenceException("Result expected");

		/*
		 * This is performance optimization. No need to loop through thousands
		 * of records if no middleware exists. We just return the result if entity
		 * is not protected. 
		 */
		if (!await IsEntityProtected(caller, result.GetType()))
			return result;

		if (result is IEntity)
		{
			await AuthorizeEntity(caller, dto, result);

			return result;
		}

		if (!result.GetType().IsEnumerable())
			return result;
		else if (result.GetType().IsEnumerable())
		{
			if (IsImmutableList(result))
				return await AuthorizeImmutableList(caller, dto, result);
			else if (IsList(result))
				return await AuthorizeList(caller, dto, result);
			else
				return result;
		}

		return result;
	}

	private async Task<TResult> AuthorizeImmutableList(ICallerContext caller, TDto dto, TResult result)
	{
		if (result is null)
			throw new NullReferenceException("Result expected");

		var resultType = result.GetType();

		if (resultType is null)
			return result;

		var field = resultType.GetField("Empty");

		if (field is null)
			return result;

		var add = resultType.GetMethod("Add");

		if (add is null)
			return result;

		var list = field.GetValue(null);
		var iterator = ((IEnumerable)result).GetEnumerator();

		while (iterator.MoveNext())
		{
			if (iterator.Current is IEntity e)
			{
				var authorizationResult = await Authorization.Authorize(caller, dto, e);

				if (authorizationResult != AuthorizationResult.Fail)
					list = add.Invoke(list, [e]);
			}
		}

		return result;
	}

	private async Task<TResult> AuthorizeList(ICallerContext caller, TDto dto, TResult result)
	{
		if (result is null)
			throw new NullReferenceException("Result expected");

		var resultType = result.GetType();

		if (resultType is null)
			return result;

		var add = resultType.GetMethod("Add");

		if (add is null)
			return result;

		var list = Activator.CreateInstance(result.GetType());
		var iterator = ((IEnumerable)result).GetEnumerator();

		while (iterator.MoveNext())
		{
			if (iterator.Current is IEntity e)
			{
				var authorizationResult = await Authorization.Authorize(caller, dto, e);

				if (authorizationResult != AuthorizationResult.Fail)
					list = add.Invoke(list, [e]);
			}
		}

		return result;
	}

	private async Task AuthorizeEntity(ICallerContext caller, TDto dto, TResult entity)
	{
		if (entity is not IEntity e)
			return;

		var result = await Authorization.Authorize(caller, dto, e);

		if (result == AuthorizationResult.Fail)
			throw new UnauthorizedAccessException(entity.GetType().Name);
	}

	private static bool IsImmutableList(TResult result)
	{
		if (result is null)
			return false;

		var type = typeof(IImmutableList<>).FullName;

		if (type is null)
			return false;

		return result.GetType().GetInterface(type) is not null;
	}

	private static bool IsList(TResult result)
	{
		if (result is null)
			return false;

		var type = typeof(IList).FullName;

		if (type is null)
			return false;

		return result.GetType().GetInterface(type) is not null;
	}

	private async Task<bool> IsEntityProtected(ICallerContext caller, object value)
	{
		var entity = ResolveEntity(value);

		if (entity is null)
			return false;

		if (EntityMap.TryGetValue(value.GetType(), out bool result))
			return result;

		var method = typeof(IAuthorizationContext).GetMethod(nameof(IAuthorizationContext.IsEntityProtected));

		if (method is null)
			return false;

		var isProtected = Convert.ToBoolean(await method.InvokeAsync(Authorization, [caller, entity]));

		EntityMap.TryAdd(entity.GetType(), isProtected);

		return isProtected;
	}

	private IEntity? ResolveEntity(object value)
	{
		if (value is IEntity)
			return value as IEntity;
		else if (value.GetType().IsEnumerable())
		{
			var en = value as IEnumerable;

			if (en is null)
				return null;

			var enm = en.GetEnumerator();

			if (enm.MoveNext() && enm.Current is not null && enm.Current is IEntity entity)
				return entity;

			return null;
		}
		else
			return null;
	}
}