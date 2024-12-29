using Connected.Authorization;
using Connected.Services.Middlewares;
using Connected.Services.Validation;
using Connected.Storage.Transactions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Connected.Services;

public abstract class Service : IService, IDisposable
{
	protected Service(IServiceProvider services)
	{
		ServiceLocator = services;

		Middleware = ServiceLocator.GetRequiredService<IMiddlewareService>();
	}

	protected IServiceProvider ServiceLocator { get; }
	private IMiddlewareService Middleware { get; }
	protected bool IsDisposed { get; private set; }

	protected async Task<TReturnValue> Invoke<TDto, TReturnValue>(IFunction<TDto, TReturnValue> function, TDto dto, [CallerMemberName] string? method = null)
		where TDto : IDto
	{
		var ctx = await Prepare(function, dto, method);
		var result = await function.Invoke(dto);

		var middleware = await Middleware.Query<IServiceFunctionMiddleware<TDto, TReturnValue>>(ctx);

		if (!middleware.IsEmpty)
		{
			foreach (var m in middleware)
			{
				if (m is ITransactionClient client && ServiceLocator.GetService<ITransactionContext>() is ITransactionContext transaction)
					transaction.Register(client);

				result = await m.Invoke(dto, result);
			}
		}

		if (result is not null)
			return await Authorize(ctx, dto, result);

#pragma warning disable 8603
		return result;
#pragma warning restore 8603
	}

	protected async Task Invoke<TDto>(IAction<TDto> action, TDto dto, [CallerMemberName] string? method = null)
		where TDto : IDto
	{
		var ctx = await Prepare(action, dto, method);

		await action.Invoke(dto);

		var middleware = await Middleware.Query<IServiceActionMiddleware<TDto>>(ctx);

		if (!middleware.IsEmpty)
		{
			foreach (var m in middleware)
			{
				if (m is ITransactionClient client && ServiceLocator.GetService<ITransactionContext>() is ITransactionContext transaction)
					transaction.Register(client);

				await m.Invoke(dto);
			}
		}
	}

	private async Task<ICallerContext> Prepare<TDto>(IServiceOperation<TDto> operation, TDto dto, [CallerMemberName] string? method = null)
		where TDto : IDto
	{
		if (operation is ITransactionClient client && ServiceLocator.GetService<ITransactionContext>() is ITransactionContext transaction)
			transaction.Register(client);

		var ctx = new CallerContext
		{
			Sender = this,
			Method = method
		};

		if (operation is ServiceOperation<TDto> op)
			op.Caller = ctx;

		await ProvideDtoValues(dto);
		await InitializeAmbient(dto);
		await Calibrate(ctx, dto);
		await Validate(ctx, dto);
		await Authorize(ctx, dto);

		return ctx;
	}

	public TOperation GetOperation<TOperation>([CallerMemberName] string? method = null)
	{
		var result = ServiceLocator.GetService<TOperation>();

		return result is null ? throw new NullReferenceException($"Service operation not found ({typeof(TOperation)})") : result;
	}

	private async Task Calibrate<TDto>(ICallerContext ctx, TDto dto)
		where TDto : IDto
	{
		var middlewares = await Middleware.Query<ICalibrator<TDto>>(ctx);

		foreach (var middleware in middlewares)
			await middleware.Invoke(dto);
	}

	private async Task Validate<TDto>(ICallerContext caller, TDto dto)
			where TDto : IDto
	{
		if (ServiceLocator.GetService<IValidationContext>() is not IValidationContext validationContext)
			return;

		await validationContext.Validate(caller, dto);
	}

	private async Task Authorize<TDto>(ICallerContext caller, TDto dto)
		where TDto : IDto
	{
		if (caller.Sender is null || ServiceLocator.GetService<IAuthorizationContext>() is not IAuthorizationContext authorization)
			return;

		await authorization.Authorize(caller, dto);
	}

	private async Task<TResult> Authorize<TDto, TResult>(ICallerContext caller, TDto dto, TResult result)
		where TDto : IDto
	{
		if (result is null)
			return result;

		if (ServiceLocator.GetService<IAuthorizationContext>() is not IAuthorizationContext authorization)
			return result;

		return await new OperationResultAuthorizer<TDto, TResult>(authorization).Authorize(caller, dto, result);
	}

	private async Task ProvideDtoValues<TDto>(TDto dto)
		where TDto : IDto
	{
		if (await Middleware.Query<IDtoValuesProvider<TDto>>() is not ImmutableList<IDtoValuesProvider<TDto>> middleware || middleware.IsEmpty)
			return;

		foreach (var m in middleware)
			await m.Invoke(dto);
	}

	private async Task InitializeAmbient<TDto>(TDto dto)
		where TDto : IDto
	{
		if (await Middleware.Query<IAmbientProvider<TDto>>() is not ImmutableList<IAmbientProvider<TDto>> middleware || middleware.IsEmpty)
			return;

		foreach (var m in middleware)
			await m.Invoke(dto);
	}

	private void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
				OnDisposing();

			IsDisposed = true;
		}
	}

	protected virtual void OnDisposing()
	{

	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}