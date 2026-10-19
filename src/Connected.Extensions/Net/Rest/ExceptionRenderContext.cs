using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace Connected.Net.Rest;

/// <inheritdoc/>
internal sealed class ExceptionRenderContext(Exception exception, HttpContext httpContext)
	: IExceptionRenderContext
{
	private static readonly JsonSerializerOptions _options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	/// <inheritdoc/>
	public Exception Exception { get; } = exception;

	/// <inheritdoc/>
	public HttpContext HttpContext { get; } = httpContext;

	/// <inheritdoc/>
	public bool IsDevelopment => HttpContext.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true;

	/// <inheritdoc/>
	public async Task Write(int statusCode, object body)
	{
		if (HttpContext.Response.HasStarted)
			return;

		var content = JsonSerializer.SerializeToUtf8Bytes(body, _options);

		HttpContext.Response.StatusCode = statusCode;
		HttpContext.Response.ContentType = "application/json";
		HttpContext.Response.ContentLength = content.Length;

		await HttpContext.Response.Body.WriteAsync(content);
	}
}
