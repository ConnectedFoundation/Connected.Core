using Microsoft.AspNetCore.Http;

namespace Connected.Net.Rest;

internal abstract class Formatter
{
	public HttpContext? Context { get; set; }

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
}
