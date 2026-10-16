using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Connected.Net.Grpc;

/// <summary>
/// Lets the <see cref="IGrpcExceptionMiddleware"/> instances decide what a call fails with when its handler throws.
/// </summary>
internal sealed class ExceptionInterceptor(IMiddlewareService middlewares)
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

	private async Task<RpcException?> Translate(Exception exception, ServerCallContext callContext)
	{
		var context = new Context(exception, callContext);

		foreach (var middleware in await middlewares.Query<IGrpcExceptionMiddleware>())
		{
			if (await middleware.Translate(context) is { } translated)
				return translated;
		}

		return null;
	}
}
