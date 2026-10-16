using Grpc.Core;
using System.Collections.Immutable;
using System.Reflection;

namespace Connected.Net.Grpc.Tests;

/// <summary>
/// Holds what the interceptor does with an exception a gRPC handler throws: which exceptions the middlewares are asked
/// about, which middleware wins, and what is thrown when none translates.
/// </summary>
[TestClass]
public sealed class ExceptionInterceptorTests
{
	private sealed class Middleware(Func<Exception, RpcException?> translate)
		: IGrpcExceptionMiddleware
	{
		public int Asked { get; private set; }

		public Task<RpcException?> Translate(IGrpcExceptionContext context)
		{
			Asked++;

			return Task.FromResult(translate(context.Exception));
		}

		public Task Initialize(CancellationToken? cancellationToken = null) => Task.CompletedTask;

		public void Dispose()
		{
		}
	}

	/// <summary>
	/// The middleware service, answering the one query the interceptor makes.
	/// </summary>
	public class MiddlewareServiceProxy
		: DispatchProxy
	{
		internal IReadOnlyList<IGrpcExceptionMiddleware> Middlewares { get; set; } = [];

		protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
		{
			if (targetMethod is { Name: "Query", IsGenericMethod: true }
				&& targetMethod.GetGenericArguments()[0] == typeof(IGrpcExceptionMiddleware))
				return Task.FromResult<IImmutableList<IGrpcExceptionMiddleware>>(Middlewares.ToImmutableList());

			throw new NotSupportedException(targetMethod?.Name);
		}
	}

	private static ExceptionInterceptor Interceptor(params IGrpcExceptionMiddleware[] middlewares)
	{
		var service = DispatchProxy.Create<IMiddlewareService, MiddlewareServiceProxy>();

		((MiddlewareServiceProxy)(object)service).Middlewares = middlewares;

		return new ExceptionInterceptor(service);
	}

	private static Task<string> Throws(Exception exception) =>
		Interceptor().UnaryServerHandler<string, string>("request", new CallContext(), (_, _) => throw exception);

	private static RpcException Failed(StatusCode code) => new(new Status(code, "translated"));

	[TestMethod]
	public async Task A_handler_that_succeeds_answers_as_it_did()
	{
		var middleware = new Middleware(_ => Failed(StatusCode.Internal));

		var result = await Interceptor(middleware).UnaryServerHandler<string, string>("request", new CallContext(), (request, _) => Task.FromResult($"{request}!"));

		Assert.AreEqual("request!", result);
		Assert.AreEqual(0, middleware.Asked);
	}

	[TestMethod]
	public async Task The_exception_a_middleware_translates_to_is_what_the_call_fails_with()
	{
		var middleware = new Middleware(_ => Failed(StatusCode.PermissionDenied));

		var error = await Assert.ThrowsExceptionAsync<RpcException>(() =>
			Interceptor(middleware).UnaryServerHandler<string, string>("request", new CallContext(), (_, _) => throw new InvalidOperationException("boom")));

		Assert.AreEqual(StatusCode.PermissionDenied, error.StatusCode);
		Assert.AreEqual(1, middleware.Asked);
	}

	[TestMethod]
	public async Task The_first_middleware_that_translates_wins_and_the_rest_are_not_asked()
	{
		var declines = new Middleware(_ => null);
		var first = new Middleware(_ => Failed(StatusCode.NotFound));
		var second = new Middleware(_ => Failed(StatusCode.Internal));

		var error = await Assert.ThrowsExceptionAsync<RpcException>(() =>
			Interceptor(declines, first, second).UnaryServerHandler<string, string>("request", new CallContext(), (_, _) => throw new InvalidOperationException()));

		Assert.AreEqual(StatusCode.NotFound, error.StatusCode);
		Assert.AreEqual(1, declines.Asked);
		Assert.AreEqual(1, first.Asked);
		Assert.AreEqual(0, second.Asked);
	}

	[TestMethod]
	public async Task When_no_middleware_translates_the_original_exception_is_thrown()
	{
		var original = new InvalidOperationException("boom");
		var middleware = new Middleware(_ => null);

		var error = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
			Interceptor(middleware).UnaryServerHandler<string, string>("request", new CallContext(), (_, _) => throw original));

		Assert.AreSame(original, error);
		Assert.AreEqual(1, middleware.Asked);
	}

	[TestMethod]
	public async Task With_no_middleware_at_all_the_original_exception_is_thrown()
	{
		var original = new InvalidOperationException("boom");

		var error = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Throws(original));

		Assert.AreSame(original, error);
	}

	[TestMethod]
	public async Task An_exception_that_is_already_a_failed_call_goes_out_as_it_is_without_asking_a_middleware()
	{
		var middleware = new Middleware(_ => Failed(StatusCode.Internal));
		var original = Failed(StatusCode.PermissionDenied);

		var error = await Assert.ThrowsExceptionAsync<RpcException>(() =>
			Interceptor(middleware).UnaryServerHandler<string, string>("request", new CallContext(), (_, _) => throw original));

		Assert.AreSame(original, error);
		Assert.AreEqual(0, middleware.Asked);
	}

	[TestMethod]
	public async Task The_other_kinds_of_call_are_translated_too()
	{
		var middleware = new Middleware(_ => Failed(StatusCode.FailedPrecondition));
		var interceptor = Interceptor(middleware);

		var client = await Assert.ThrowsExceptionAsync<RpcException>(() =>
			interceptor.ClientStreamingServerHandler<string, string>(null!, new CallContext(), (_, _) => throw new InvalidOperationException()));
		var server = await Assert.ThrowsExceptionAsync<RpcException>(() =>
			interceptor.ServerStreamingServerHandler<string, string>("request", null!, new CallContext(), (_, _, _) => throw new InvalidOperationException()));
		var duplex = await Assert.ThrowsExceptionAsync<RpcException>(() =>
			interceptor.DuplexStreamingServerHandler<string, string>(null!, null!, new CallContext(), (_, _, _) => throw new InvalidOperationException()));

		Assert.AreEqual(StatusCode.FailedPrecondition, client.StatusCode);
		Assert.AreEqual(StatusCode.FailedPrecondition, server.StatusCode);
		Assert.AreEqual(StatusCode.FailedPrecondition, duplex.StatusCode);
		Assert.AreEqual(3, middleware.Asked);
	}

	/// <summary>
	/// A call the interceptor passes on without looking at.
	/// </summary>
	private sealed class CallContext
		: ServerCallContext
	{
		protected override string MethodCore => "/test.Service/Method";
		protected override string HostCore => "localhost";
		protected override string PeerCore => "peer";
		protected override DateTime DeadlineCore => DateTime.MaxValue;
		protected override Metadata RequestHeadersCore => [];
		protected override CancellationToken CancellationTokenCore => CancellationToken.None;
		protected override Metadata ResponseTrailersCore => [];
		protected override Status StatusCore { get; set; }
		protected override WriteOptions? WriteOptionsCore { get; set; }
		protected override AuthContext AuthContextCore => new(null, new Dictionary<string, List<AuthProperty>>());

		protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
			throw new NotSupportedException();

		protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
	}
}
