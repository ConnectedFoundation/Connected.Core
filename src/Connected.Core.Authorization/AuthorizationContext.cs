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
	public bool IsAuthorized { get; set; }

	public void Reset()
	{
		IsAuthorized = false;
	}

	private async Task<AuthorizationResult> Authorize()
	{
		if (http?.HttpContext is null)
			return AuthorizationResult.Skip;

		var middlewares = await middleware.Query<IHttpRequestAuthorization>();
		var onePassed = false;

		foreach (var middleware in middlewares)
		{
			var decorationResult = await InvokeDecorations(middleware);

			if (decorationResult == AuthorizationResult.Fail)
				return AuthorizationResult.Fail;
			else if (decorationResult == AuthorizationResult.Pass)
				onePassed = true;

			var result = await middleware.Invoke();

			if (result == AuthorizationResult.Fail)
				return AuthorizationResult.Fail;

			if (result == AuthorizationResult.Pass)
				onePassed = true;

			if (middleware.IsSealed)
				break;
		}

		if (middlewares.Count != 0 && onePassed)
			IsAuthorized = true;

		if (onePassed)
			return AuthorizationResult.Pass;

		return AuthorizationResult.Skip;
	}

	public async Task<AuthorizationResult> Authorize<TDto>(ICallerContext context, TDto dto)
		where TDto : IDto
	{
		IsAuthorized = false;

		var operationResult = await AuthorizeServiceOperation(context, dto);

		if (operationResult == AuthorizationResult.Fail)
			return AuthorizationResult.Fail;

		if (IsAuthorized)
			return AuthorizationResult.Pass;

		var serviceResult = await AuthorizeService(context, dto);

		if (serviceResult == AuthorizationResult.Fail)
			return AuthorizationResult.Fail;

		if (IsAuthorized)
			return AuthorizationResult.Pass;

		return await Authorize();
	}

	private async Task<AuthorizationResult> AuthorizeService<TDto>(ICallerContext context, TDto dto)
		where TDto : IDto
	{
		/*
		 * We need to strip method from the caller since we are looking for 
		 * the service scope middleware.
		 */
		var caller = new CallerContext { Sender = context.Sender };
		var middlewares = await middleware.Query<IServiceAuthorization>(caller);
		var authorizationDto = Dto.Factory.Create<IServiceAuthorizationDto>();
		var onePassed = false;

		authorizationDto.Caller = context;
		authorizationDto.Dto = dto;

		foreach (var middleware in middlewares)
		{
			var decorationResult = await InvokeDecorations(middleware);

			if (decorationResult == AuthorizationResult.Fail)
				return AuthorizationResult.Fail;

			if (decorationResult == AuthorizationResult.Pass)
				onePassed = true;

			var result = await middleware.Invoke(authorizationDto);

			if (result == AuthorizationResult.Fail)
				return AuthorizationResult.Fail;

			if (result == AuthorizationResult.Pass)
				onePassed = true;

			if (middleware.IsSealed)
				break;
		}

		if (middlewares.Count != 0 && onePassed)
			IsAuthorized = true;

		if (onePassed)
			return AuthorizationResult.Pass;

		return AuthorizationResult.Skip;
	}

	private async Task<AuthorizationResult> AuthorizeServiceOperation<TDto>(ICallerContext context, TDto dto) where TDto : IDto
	{
		var middlewares = await middleware.Query<IServiceOperationAuthorization<TDto>>(context);
		var operationDto = Dto.Factory.Create<IServiceOperationAuthorizationDto<TDto>>();
		var onePassed = false;

		operationDto.Caller = context;
		operationDto.Dto = dto;

		foreach (var middleware in middlewares)
		{
			var decorationResult = await InvokeDecorations(middleware);

			if (decorationResult == AuthorizationResult.Fail)
				return AuthorizationResult.Fail;
			else if (decorationResult == AuthorizationResult.Pass)
				onePassed = true;

			var result = await middleware.Invoke(operationDto);

			if (result == AuthorizationResult.Fail)
				return AuthorizationResult.Fail;
			else if (result == AuthorizationResult.Pass)
				onePassed = true;

			if (middleware.IsSealed)
				break;
		}

		if (middlewares.Count != 0 && onePassed)
			IsAuthorized = true;

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
		var middlewares = await middleware.Query<IEntityAuthorization<TEntity>>(context);
		var entityDto = Dto.Factory.Create<IEntityAuthorizationDto<TDto, TEntity>>();
		var onePassed = false;

		entityDto.Caller = context;
		entityDto.Dto = dto;
		entityDto.Entity = entity;

		foreach (var middleware in middlewares)
		{
			var decorationsResult = await InvokeDecorations(middleware);

			if (decorationsResult == AuthorizationResult.Fail)
				return AuthorizationResult.Fail;
			else if (decorationsResult == AuthorizationResult.Pass)
				onePassed = true;

			var authorizationResult = await middleware.Invoke(entityDto);

			if (authorizationResult == AuthorizationResult.Fail)
				return AuthorizationResult.Fail;
			else if (authorizationResult == AuthorizationResult.Pass)
				onePassed = true;

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
		return (await middleware.Query<IEntityAuthorization<TEntity>>(context)).Any();
	}

	private async Task<AuthorizationResult> InvokeDecorations(IAuthorization instance)
	{
		var handlers = await middleware.Query<IAuthorizationDecorationHandler>();
		var dto = Dto.Factory.Create<IAuthorizationDecorationHandlerDto>();
		var onePassed = false;

		dto.Middleware = instance;

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
