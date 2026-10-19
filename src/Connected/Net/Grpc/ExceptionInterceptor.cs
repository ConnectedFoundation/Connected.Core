using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Connected.Net.Grpc;

/// <summary>
/// Lets the <see cref="IGrpcExceptionMiddleware"/> instances decide what a call fails with when its handler throws.
/// </summary>
internal sealed class ExceptionInterceptor(IMiddlewareService middlewares, ILogger<ExceptionInterceptor> logger)
	: Interceptor
{
	private sealed class Context(Exception exception, ServerCallContext callContext)
		: IGrpcExceptionContext
	{
		public Exception Exception { get; } = exception;
		public ServerCallContext CallContext { get; } = callContext;
	}

	public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context,
		UnaryServerMethod<TRequest, TResponse> continuation)
	{
		try
		{
			return await continuation(request, context);
		}
		catch (Exception ex) when (ex is not RpcException)
		{
			var translated = await Translate(ex, context);

			if (translated is null)
				throw;

			throw translated;
		}
	}

	public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream,
		ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
	{
		try
		{
			return await continuation(requestStream, context);
		}
		catch (Exception ex) when (ex is not RpcException)
		{
			var translated = await Translate(ex, context);

			if (translated is null)
				throw;

			throw translated;
		}
	}

	public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream,
		ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
	{
		try
		{
			await continuation(request, responseStream, context);
		}
		catch (Exception ex) when (ex is not RpcException)
		{
			var translated = await Translate(ex, context);

			if (translated is null)
				throw;

			throw translated;
		}
	}

	public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream,
		IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
	{
		try
		{
			await continuation(requestStream, responseStream, context);
		}
		catch (Exception ex) when (ex is not RpcException)
		{
			var translated = await Translate(ex, context);

			if (translated is null)
				throw;

			throw translated;
		}
	}

	/// <summary>
	/// Asks the middlewares, in the order they are given, what the call fails with.
	/// </summary>
	/// <remarks>
	/// A middleware that fails is not allowed to replace the exception the handler threw: the failure is logged,
	/// together with that exception, and the next middleware is asked. When none translates it, the caller throws the
	/// original exception.
	/// </remarks>
	private async Task<RpcException?> Translate(Exception exception, ServerCallContext callContext)
	{
		var context = new Context(exception, callContext);
		IEnumerable<IGrpcExceptionMiddleware> candidates;

		try
		{
			candidates = await middlewares.Query<IGrpcExceptionMiddleware>();
		}
		catch (Exception failure)
		{
			logger.LogError(failure, "The gRPC exception middlewares could not be resolved. The exception is left as it is.");
			logger.LogError(exception, "The call failed with this exception.");

			return null;
		}

		foreach (var middleware in candidates)
		{
			try
			{
				if (await middleware.Translate(context) is { } translated)
					return translated;
			}
			catch (Exception failure)
			{
				logger.LogError(failure, "The gRPC exception middleware {Middleware} failed. The next one is asked.", middleware.GetType().Name);
				logger.LogError(exception, "The call failed with this exception.");
			}
		}

		return null;
	}
}
