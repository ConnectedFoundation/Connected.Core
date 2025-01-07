using Connected.Authorization.Entities;
using Connected.Authorization.Net;
using Connected.Authorization.Services;
using Connected.Entities;
using Connected.Services;
using Microsoft.AspNetCore.Http;

namespace Connected.Authorization;

internal sealed class AuthorizationContext(IAuthorizationService authorization, IMiddlewareService middleware, IHttpContextAccessor? http)
	: IAuthorizationContext
{
	private bool RequestAuthorized { get; set; }

	public async Task<IAuthorizationResult> Authorize(IAuthorizationDto dto)
	{
		return await authorization.Authorize(dto);
	}

	private async Task Authorize()
	{
		if (RequestAuthorized)
			return;

		if (http?.HttpContext is null)
			return;

		var middlewares = await middleware.Query<IHttpRequestAuthorization>();

		foreach (var middleware in middlewares)
		{
			await middleware.Invoke();

			if (middleware.IsSealed)
				break;
		}
	}

	public async Task Authorize<TDto>(ICallerContext context, TDto dto)
		where TDto : IDto
	{
		if (await AuthorizeServiceOperation(context, dto))
			return;

		if (await AuthorizeService(context, dto))
			return;

		await Authorize();
	}

	private async Task<bool> AuthorizeService<TDto>(ICallerContext context, TDto dto)
		where TDto : IDto
	{
		/*
		 * We need to strip method from the caller since we are looking for 
		 * the service scope middleware.
		 */
		var caller = new CallerContext { Sender = context.Sender };
		var middlewares = await middleware.Query<IServiceAuthorization>(caller);
		var authorizationDto = Dto.Factory.Create<IServiceAuthorizationDto>();

		authorizationDto.Caller = context;
		authorizationDto.Dto = dto;

		foreach (var middleware in middlewares)
		{
			await middleware.Invoke(authorizationDto);

			if (middleware.IsSealed)
				break;
		}

		return !middlewares.IsEmpty;
	}

	private async Task<bool> AuthorizeServiceOperation<TDto>(ICallerContext context, TDto dto) where TDto : IDto
	{
		var middlewares = await middleware.Query<IServiceOperationAuthorization<TDto>>(context);
		var operationDto = Dto.Factory.Create<IServiceOperationAuthorizationDto<TDto>>();

		operationDto.Caller = context;
		operationDto.Dto = dto;

		foreach (var middleware in middlewares)
		{
			await middleware.Invoke(operationDto);

			if (middleware.IsSealed)
				break;
		}

		return !middlewares.IsEmpty;
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
	public async Task<TEntity?> Authorize<TDto, TEntity>(ICallerContext context, TDto dto, TEntity entity)
		where TDto : IDto
		where TEntity : IEntity
	{
		var middlewares = await middleware.Query<IEntityAuthorization<TEntity>>(context);
		var entityDto = Dto.Factory.Create<IEntityAuthorizationDto<TDto, TEntity>>();

		entityDto.Caller = context;
		entityDto.Dto = dto;
		entityDto.Entity = entity;

		var result = entity;

		foreach (var middleware in middlewares)
		{
			result = await middleware.Invoke(entityDto);

			if (result is null)
				return default;

			entityDto.Entity = result;

			if (middleware.IsSealed)
				break;
		}

		return result;
	}

	public async Task<bool> IsEntityProtected<TEntity>(ICallerContext context, TEntity entity)
		where TEntity : IEntity
	{
		return (await middleware.Query<IEntityAuthorization<TEntity>>(context)).Any();
	}
}
