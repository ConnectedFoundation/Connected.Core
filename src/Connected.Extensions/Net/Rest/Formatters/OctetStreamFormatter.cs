using System.Text.Json;

namespace Connected.Net.Rest.Formatters;

internal class OctetStreamFormatter : Formatter
{
	public const string ContentType = "application/octet-stream";
	private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	protected override async Task<IDictionary<string, object?>?> OnParseArguments()
	{
		if (Context is null)
			return null;

		var items = new Dictionary<string, object?>();

		foreach (var header in Context.Request.Headers)
		{
			if (header.Key.ToLowerInvariant().StartsWith("x-tp-param-"))
				items.Add(header.Key[11..], header.Value.ToString());
		}

		await Task.CompletedTask;

		return WithRouteValues(items);
	}

	protected override async Task OnRenderResult(object? content)
	{
		if (Context is null)
			return;

		if (!Context.Response.HasStarted)
		{
			var length = 0L;
			byte[]? buffer = null;

			if (content is not null)
			{
				using var ms = new MemoryStream();

				await JsonSerializer.SerializeAsync(ms, content, _options);

				ms.Seek(0, SeekOrigin.Begin);

				length = ms.Length;
				buffer = ms.ToArray();
			}

			Context.Response.ContentLength = length;

			if (length == 0)
				Context.Response.ContentType = "text/plain";
			else
				Context.Response.ContentType = ContentType;

			if (buffer is not null)
				await Context.Response.Body.WriteAsync(buffer);
		}

		await Context.Response.CompleteAsync();
	}
}
