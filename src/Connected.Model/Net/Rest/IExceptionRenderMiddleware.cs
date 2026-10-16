using Microsoft.AspNetCore.Http;

namespace Connected.Net.Rest;

/// <summary>
/// Defines a middleware that renders the exception a REST request failed with.
/// </summary>
/// <remarks>
/// <para>
/// By default a failed request is answered with a status derived from the exception type and a body that holds the
/// exception message. A middleware replaces that: the first one that returns <c>true</c> from
/// <see cref="Render"/> has answered the request, and the default rendering is skipped. When none does, the default
/// applies, so an application that registers no middleware sees no difference.
/// </para>
/// <para>
/// The middleware owns the whole answer. It may change the status code and write any body, so what a client
/// receives is not limited to what the framework knows about.
/// </para>
/// </remarks>
public interface IExceptionRenderMiddleware
	: IMiddleware
{
	/// <summary>
	/// Renders the exception, or declines to.
	/// </summary>
	/// <param name="context">The failed request and the exception it failed with.</param>
	/// <returns><c>true</c> when the exception was rendered; <c>false</c> to leave it to the next middleware or to the
	/// default rendering.</returns>
	Task<bool> Render(IExceptionRenderContext context);
}

/// <summary>
/// What an <see cref="IExceptionRenderMiddleware"/> works with.
/// </summary>
public interface IExceptionRenderContext
{
	/// <summary>
	/// Gets the exception the request failed with.
	/// </summary>
	Exception Exception { get; }

	/// <summary>
	/// Gets the failed request and its response, for a middleware that needs more than <see cref="Write"/>.
	/// </summary>
	HttpContext HttpContext { get; }

	/// <summary>
	/// Gets a value indicating whether the application runs in the development environment, where an answer may
	/// carry detail that must never reach a client in production.
	/// </summary>
	bool IsDevelopment { get; }

	/// <summary>
	/// Answers the request with a status code and a JSON body.
	/// </summary>
	/// <param name="statusCode">The HTTP status code.</param>
	/// <param name="body">Anything the JSON serializer can write: an anonymous object, a dictionary, a record.
	/// Property names are written in camel case.</param>
	Task Write(int statusCode, object body);
}
