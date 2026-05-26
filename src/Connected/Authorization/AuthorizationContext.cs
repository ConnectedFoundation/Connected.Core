using Connected.Authorization.Dtos;
using Connected.Authorization.Entities;
using Connected.Authorization.Net;
using Connected.Authorization.Services;
using Connected.Entities;
using Connected.Services;
using Microsoft.AspNetCore.Http;

namespace Connected.Authorization;

internal sealed class AuthorizationContext(IMiddlewareService middleware, IHttpContextAccessor? http)
	: IAuthorizationContext
{
	private static readonly Dictionary<Type, bool> _entityCache = [];

	private async Task<AuthorizationResult> Authorize()
	{
		if (http?.HttpContext is null)
			return AuthorizationResult.Fail;

		var middlewares = await middleware.Query<IHttpRequestAuthorization>();
		var onePassed = false;

		foreach (var middleware in middlewares)
		{
			var result = await middleware.Invoke();

			if (result == AuthorizationResult.Fail)
				return AuthorizationResult.Fail;
			else if (result == AuthorizationResult.Pass)
				onePassed = true;
			else
			{
				var decorationResult = await InvokeDecorations(middleware);

				if (decorationResult == AuthorizationResult.Fail)
					return AuthorizationResult.Fail;
				else if (decorationResult == AuthorizationResult.Pass)
					onePassed = true;
			}

			if (middleware.IsSealed)
				break;
		}

		if (onePassed)
			return AuthorizationResult.Pass;

		return AuthorizationResult.Fail;
	}

	public async Task<AuthorizationResult> Authorize<TDto>(ICallerContext context, TDto dto)
		where TDto : IDto
	{
		var result = await AuthorizeServiceOperation(context, dto);

		if (result == AuthorizationResult.Fail)
			return AuthorizationResult.Fail;
		else if (result == AuthorizationResult.Pass)
			return AuthorizationResult.Pass;

		result = await AuthorizeService(context, dto);

		if (result == AuthorizationResult.Fail)
			return AuthorizationResult.Fail;
		else if (result == AuthorizationResult.Pass)
			return AuthorizationResult.Pass;

		result = await AuthorizeScope(context, dto);

		if (result == AuthorizationResult.Fail)
			return AuthorizationResult.Fail;
		else if (result == AuthorizationResult.Pass)
			return AuthorizationResult.Pass;

		return await Authorize();
	}

	private async Task<AuthorizationResult> AuthorizeScope<TDto>(ICallerContext context, TDto dto)
		where TDto : IDto
	{
		/*
		 * We need to strip method from the caller since we are looking for 
		 * the service scope middleware.
		 */
		var onePassed = false;
		var middlewares = await middleware.Query<IScopeAuthorization>();
		var authorizationDto = DtoFactory.Create<IScopeAuthorizationDto>((f) =>
		{
			f.Caller = context;
			f.Dto = dto;
		});

		foreach (var middleware in middlewares)
		{
			var result = await middleware.Invoke(authorizationDto);

			if (result == AuthorizationResult.Fail)
				return AuthorizationResult.Fail;
			else if (result == AuthorizationResult.Pass)
				onePassed = true;
			else
			{
				var decorationResult = await InvokeDecorations(middleware);

				if (decorationResult == AuthorizationResult.Fail)
					return AuthorizationResult.Fail;
				else if (decorationResult == AuthorizationResult.Pass)
					onePassed = true;
			}

			if (middleware.IsSealed)
				break;
		}

		if (onePassed)
			return AuthorizationResult.Pass;

		return AuthorizationResult.Skip;
	}
	private async Task<AuthorizationResult> AuthorizeService<TDto>(ICallerContext context, TDto dto)
		where TDto : IDto
	{
		/*
		 * We need to strip method from the caller since we are looking for 
		 * the service scope middleware.
		 */
		var caller = new CallerContext { Sender = context.Sender };
		var onePassed = false;
		var middlewares = await middleware.Query<IServiceAuthorization>(caller);
		var authorizationDto = DtoFactory.Create<IServiceAuthorizationDto>((f) =>
		{
			f.Caller = context;
			f.Dto = dto;
		});

		foreach (var middleware in middlewares)
		{
			var result = await middleware.Invoke(authorizationDto);

			if (result == AuthorizationResult.Fail)
				return AuthorizationResult.Fail;
			else if (result == AuthorizationResult.Pass)
				onePassed = true;
			else
			{
				var decorationResult = await InvokeDecorations(middleware);

				if (decorationResult == AuthorizationResult.Fail)
					return AuthorizationResult.Fail;
				else if (decorationResult == AuthorizationResult.Pass)
					onePassed = true;
			}

			if (middleware.IsSealed)
				break;
		}

		if (onePassed)
			return AuthorizationResult.Pass;

		return AuthorizationResult.Skip;
	}

	private async Task<AuthorizationResult> AuthorizeServiceOperation<TDto>(ICallerContext context, TDto dto) where TDto : IDto
	{
		var onePassed = false;
		var middlewares = await middleware.Query<IServiceOperationAuthorization<TDto>>(context);
		var operationDto = DtoFactory.Create<IServiceOperationAuthorizationDto<TDto>>((f) =>
		{
			f.Caller = context;
			f.Dto = dto;
		});

		foreach (var middleware in middlewares)
		{
			var result = await middleware.Invoke(operationDto);

			if (result == AuthorizationResult.Fail)
				return AuthorizationResult.Fail;
			else if (result == AuthorizationResult.Pass)
				onePassed = true;
			else
			{
				var decorationResult = await InvokeDecorations(middleware);

				if (decorationResult == AuthorizationResult.Fail)
					return AuthorizationResult.Fail;
				else if (decorationResult == AuthorizationResult.Pass)
					onePassed = true;
			}

			if (middleware.IsSealed)
				break;
		}

		if (onePassed)
			return AuthorizationResult.Pass;

		return AuthorizationResult.Skip;
	}
	/// <summary>
	/// Results are always authorized, event if <see cref="State"/> is <see cref="AuthorizationContextState.Granted"/>.
	/// </summary>
	/// <typeparam name="TDto"></typeparam>
	/// <typeparam name="TEntity"></typeparam>
	/// <param name="context"></param>
	/// <param name="dto"></param>
	/// <param name="component"></param>
	/// <returns></returns>
	public async Task<AuthorizationResult> Authorize<TDto, TEntity>(ICallerContext context, TDto dto, TEntity entity)
		where TDto : IDto
		where TEntity : IEntity
	{
		if (!await IsEntityProtected(context, entity))
			return AuthorizationResult.Pass;

		var onePassed = false;
		var middlewares = await middleware.Query<IEntityAuthorization<TEntity>>(context);
		var entityDto = DtoFactory.Create<IEntityAuthorizationDto<TDto, TEntity>>((f) =>
		{
			f.Caller = context;
			f.Entity = entity;
			f.Dto = dto;
		});

		foreach (var middleware in middlewares)
		{
			var authorizationResult = await middleware.Invoke(entityDto);

			if (authorizationResult == AuthorizationResult.Fail)
				return AuthorizationResult.Fail;
			else if (authorizationResult == AuthorizationResult.Pass)
				onePassed = true;
			else
			{
				var decorationsResult = await InvokeDecorations(middleware);

				if (decorationsResult == AuthorizationResult.Fail)
					return AuthorizationResult.Fail;
				else if (decorationsResult == AuthorizationResult.Pass)
					onePassed = true;
			}

			if (middleware.IsSealed)
				break;
		}

		if (onePassed)
			return AuthorizationResult.Pass;

		return AuthorizationResult.Skip;
	}

	public async Task<bool> IsEntityProtected<TEntity>(ICallerContext context, TEntity entity)
		where TEntity : IEntity
	{
		if (_entityCache.TryGetValue(typeof(TEntity), out var entityDto))
			return entityDto;

		var result = (await middleware.Query<IEntityAuthorization<TEntity>>(context)).Any();

		_entityCache[typeof(TEntity)] = result;

		return result;
	}

	private async Task<AuthorizationResult> InvokeDecorations(IAuthorization instance)
	{
		var onePassed = false;
		var handlers = await middleware.Query<IAuthorizationDecorationHandler>();
		var dto = DtoFactory.Create<IAuthorizationDecorationHandlerDto>((f) =>
		{
			f.Middleware = instance;
		});

		foreach (var handler in handlers)
		{
			var result = await handler.Invoke(dto);

			if (result == AuthorizationResult.Fail)
				return result;
			else if (result == AuthorizationResult.Pass)
				onePassed = true;
		}

		if (onePassed)
			return AuthorizationResult.Pass;

		return AuthorizationResult.Skip;
	}
}
