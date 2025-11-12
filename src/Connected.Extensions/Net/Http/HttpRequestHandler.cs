using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Net;

namespace Connected.Net.Http;

public abstract class HttpRequestHandler(HttpContext context) : IDisposable
{
	protected HttpContext Context { get; } = context;

	public async Task Invoke()
	{
		await OnInvoke();
	}

	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}

	protected bool HasBeenModified(DateTimeOffset date)
	{
		date = new DateTimeOffset(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, TimeSpan.Zero);
		var ifModified = Context.Request.Headers.IfModifiedSince.ToString();
		var etag = Context.Request.Headers.ETag.ToString();

		if (string.IsNullOrWhiteSpace(ifModified))
			ifModified = null;

		if (string.IsNullOrWhiteSpace(etag))
			etag = null;

		if (ifModified is not null)
		{
			var provider = CultureInfo.InvariantCulture;
			var lastMod = DateTimeOffset.ParseExact(ifModified, "r", provider).ToLocalTime();

			if (lastMod == date)
			{
				Context.Response.StatusCode = 304;

				return false;
			}
		}
		else if (string.IsNullOrEmpty(etag))
		{
			var lastMod = new DateTimeOffset(Convert.ToInt64(ifModified), TimeSpan.Zero);

			if (lastMod == date)
			{
				Context.Response.StatusCode = 304;

				return false;
			}
		}
		return true;
	}

	protected void SetModified(DateTimeOffset date, int maxAge = 600)
	{
		date = new DateTimeOffset(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, TimeSpan.Zero);

		Context.Response.Headers.LastModified = date.ToUniversalTime().ToString("r");
		Context.Response.Headers.ETag = date.ToUniversalTime().Ticks.ToString();
		Context.Response.Headers.CacheControl = $"public, max-age={maxAge}";
	}

	protected async Task Write(DateTimeOffset modified, string contentType, byte[]? content)
	{
		if (content is null || content.Length == 0)
			return;

		if (modified != DateTimeOffset.MinValue && !HasBeenModified(modified))
			return;

		Context.Response.ContentType = contentType;

		if (modified != DateTimeOffset.MinValue)
			SetModified(modified);

		Context.Response.ContentLength = content.Length;

		await Context.Response.Body.WriteAsync(content);
		await Context.Response.CompleteAsync();
	}

	protected void NotFound()
	{
		Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
	}

	protected void ServerError()
	{
		Context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);

		OnDispose(true);
	}

	protected virtual void OnDispose(bool disposing)
	{

	}
}