using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Net.Rest;

internal abstract class Formatter
{
	public HttpContext? Context { get; set; }
	public AsyncServiceScope? Scope { get; set; }

	public async Task<IDictionary<string, object?>?> ParseArguments()
	{
		return await OnParseArguments();
	}

	protected abstract Task<IDictionary<string, object?>?> OnParseArguments();

	public async Task RenderResult(object? content)
	{
		await OnRenderResult(content);
	}

	protected abstract Task OnRenderResult(object? content);

	public async Task RenderException(Exception ex)
	{
		await OnRenderError(ex);
	}

	protected virtual async Task OnRenderError(Exception ex)
	{
		await Task.CompletedTask;
	}
}
