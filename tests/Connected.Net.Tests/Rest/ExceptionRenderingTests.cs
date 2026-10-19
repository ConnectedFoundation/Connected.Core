using Connected.Net.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;

namespace Connected.Net.Rest.Tests;

/// <summary>
/// Holds how a failed REST request is rendered when middlewares are registered: which one renders it, that a failing
/// middleware changes nothing for the others or for the default, and that what a middleware is handed is what was thrown.
/// </summary>
[TestClass]
public sealed class ExceptionRenderingTests
{
	private sealed class Middleware(Func<IExceptionRenderContext, Task<bool>> render)
		: IExceptionRenderMiddleware
	{
		public int Asked { get; private set; }

		public Task<bool> Render(IExceptionRenderContext context)
		{
			Asked++;

			return render(context);
		}

		public Task Initialize(CancellationToken? cancellationToken = null) => Task.CompletedTask;

		public void Dispose()
		{
		}
	}

	private sealed class Rendering
	{
		public required DefaultHttpContext Http { get; init; }
		public required CapturingLogger<ServiceRequestDelegate> Logger { get; init; }
		public required MiddlewareServiceProxy? Service { get; init; }

		public string Body
		{
			get
			{
				Http.Response.Body.Position = 0;

				return Encoding.UTF8.GetString(((MemoryStream)Http.Response.Body).ToArray());
			}
		}
	}

	/// <summary>
	/// Renders the exception through the real request path, with the given middlewares registered, or with no
	/// middleware service at all when <paramref name="register"/> is false.
	/// </summary>
	private static async Task<Rendering> Render(Exception exception, bool register, params IExceptionRenderMiddleware[] middlewares)
	{
		var logger = new CapturingLogger<ServiceRequestDelegate>();
		var services = new ServiceCollection().AddSingleton<ILogger<ServiceRequestDelegate>>(logger);
		MiddlewareServiceProxy? proxy = null;

		if (register)
		{
			var (service, created) = MiddlewareServiceProxy.Create(middlewares);

			proxy = created;
			services.AddSingleton(service);
		}

		var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

		http.Response.Body = new MemoryStream();

		var rendering = new Rendering { Http = http, Logger = logger, Service = proxy };

		await Invoke(rendering, exception);

		return rendering;
	}

	private static async Task Invoke(Rendering rendering, Exception exception)
	{
		var request = new ServiceRequestDelegate(rendering.Http);
		var method = typeof(ServiceRequestDelegate).GetMethod("OnRenderException", BindingFlags.Instance | BindingFlags.NonPublic)!;

		await (Task)method.Invoke(request, [exception])!;
	}

	private static Middleware Renders(int status, string body) =>
		new(async context =>
		{
			await context.Write(status, new { rendered = body });

			return true;
		});

	[TestMethod]
	public async Task A_middleware_that_renders_the_exception_answers_the_request_instead_of_the_default()
	{
		var middleware = Renders(409, "by the middleware");

		var result = await Render(new InvalidOperationException("boom"), true, middleware);

		Assert.AreEqual(409, result.Http.Response.StatusCode);
		Assert.AreEqual("""{"rendered":"by the middleware"}""", result.Body);
		Assert.AreEqual(1, middleware.Asked);
	}

	[TestMethod]
	public async Task A_middleware_that_declines_leaves_the_default_rendering()
	{
		var middleware = new Middleware(_ => Task.FromResult(false));

		var result = await Render(new InvalidOperationException("boom"), true, middleware);

		Assert.AreEqual("""{"message":"boom"}""", result.Body);
		Assert.AreEqual(1, middleware.Asked);
	}

	[TestMethod]
	public async Task With_no_middleware_service_the_default_rendering_applies()
	{
		var result = await Render(new InvalidOperationException("boom"), false);

		Assert.AreEqual("""{"message":"boom"}""", result.Body);
	}

	[TestMethod]
	public async Task The_first_middleware_that_renders_wins_and_the_rest_are_not_asked()
	{
		var declines = new Middleware(_ => Task.FromResult(false));
		var first = Renders(400, "first");
		var second = Renders(500, "second");

		var result = await Render(new InvalidOperationException("boom"), true, declines, first, second);

		Assert.AreEqual("""{"rendered":"first"}""", result.Body);
		Assert.AreEqual(0, second.Asked);
	}

	[TestMethod]
	public async Task A_middleware_that_throws_is_skipped_and_the_next_one_renders()
	{
		var failing = new Middleware(_ => throw new FormatException("middleware broke"));
		var next = Renders(409, "next");

		var result = await Render(new InvalidOperationException("boom"), true, failing, next);

		Assert.AreEqual("""{"rendered":"next"}""", result.Body);
		Assert.IsTrue(result.Logger.Entries.Any(f => f.Level == LogLevel.Error && f.Exception is FormatException));
	}

	[TestMethod]
	public async Task When_every_middleware_throws_the_original_exception_is_rendered_by_default_and_both_are_logged()
	{
		var original = new InvalidOperationException("boom");
		var failing = new Middleware(_ => throw new FormatException("middleware broke"));

		var result = await Render(original, true, failing);

		Assert.AreEqual("""{"message":"boom"}""", result.Body);
		Assert.IsTrue(result.Logger.Entries.Any(f => f.Exception is FormatException));
		Assert.IsTrue(result.Logger.Entries.Any(f => ReferenceEquals(f.Exception, original)));
	}

	[TestMethod]
	public async Task When_the_middlewares_cannot_be_resolved_the_original_exception_is_rendered_by_default()
	{
		var original = new InvalidOperationException("boom");
		var logger = new CapturingLogger<ServiceRequestDelegate>();
		var (service, proxy) = MiddlewareServiceProxy.Create();

		proxy.QueryFails = new TimeoutException("middleware store");

		var http = new DefaultHttpContext
		{
			RequestServices = new ServiceCollection()
				.AddSingleton<ILogger<ServiceRequestDelegate>>(logger)
				.AddSingleton(service)
				.BuildServiceProvider()
		};

		http.Response.Body = new MemoryStream();

		var rendering = new Rendering { Http = http, Logger = logger, Service = proxy };

		await Invoke(rendering, original);

		Assert.AreEqual("""{"message":"boom"}""", rendering.Body);
		Assert.IsTrue(logger.Entries.Any(f => f.Exception is TimeoutException));
	}

	[TestMethod]
	public async Task An_exception_wrapped_by_reflection_is_unwrapped_before_a_middleware_sees_it()
	{
		var inner = new ArgumentOutOfRangeException("thrown before the task was returned");
		Exception? seen = null;
		var middleware = new Middleware(context =>
		{
			seen = context.Exception;

			return Task.FromResult(false);
		});

		await Render(new TargetInvocationException(inner), true, middleware);

		Assert.AreSame(inner, seen);
	}

	[TestMethod]
	public async Task An_exception_that_is_not_wrapped_is_handed_over_as_it_is()
	{
		var original = new InvalidOperationException("boom");
		Exception? seen = null;
		var middleware = new Middleware(context =>
		{
			seen = context.Exception;

			return Task.FromResult(false);
		});

		await Render(original, true, middleware);

		Assert.AreSame(original, seen);
	}
}
