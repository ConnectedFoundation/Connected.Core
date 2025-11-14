using Connected.Authentication;
using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Connected.Net.Rest;
public abstract class RestRequest : IDisposable
{
	private AsyncServiceScope? _scope = null;

	protected RestRequest(HttpContext httpContext)
	{
		HttpContext = httpContext;

		HttpContext.RequestAborted.Register(async () =>
		{
			try
			{
				await Scope.Cancel();
			}
			catch
			{
				// avoid craching process by eating this one
			}
		});
	}

	protected bool IsDisposed { get; set; }
	protected HttpContext HttpContext { get; }
	protected AsyncServiceScope? Scope => _scope;

	public async Task Invoke()
	{
		try
		{
			_scope = await Services.Scope.Create().WithRequestIdentity();

			var result = await OnInvoke();
			/*
			 * Now, commit changes made in the context.
			 */
			await Scope.Commit();
			/*
			 * Send result to the client.
			 */
			await OnRenderResult(result);
		}
		catch (Exception ex)
		{
			await Scope.Rollback();
			await HandleException(ex);
		}
	}

	protected virtual async Task<object?> OnInvoke()
	{
		return await Task.FromResult<object?>(null);
	}

	protected virtual async Task OnRenderResult(object? result)
	{
		await Task.CompletedTask;
	}
	private async Task HandleException(Exception ex)
	{
		if (HttpContext.Response.StatusCode != (int)HttpStatusCode.OK)
			return;

		if (ex is AccessViolationException)
			HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
		else if (ex is UnauthorizedAccessException)
			HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
		else if (ex is ValidationException)
			HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
		else if (ex is ArgumentException)
			HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
		else if (ex is NullReferenceException)
			HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
		else if (ex is InvalidOperationException)
			HttpContext.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
		else
			HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

		await OnRenderException(ex);
	}

	protected virtual async Task OnRenderException(Exception ex)
	{
		await Task.CompletedTask;
	}

	private void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				if (_scope is not null)
				{
					_scope.Value.Dispose();
					_scope = null;
				}

				OnDispose();
			}

			IsDisposed = true;
		}
	}

	protected virtual void OnDispose()
	{

	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
