using System.Text.Json;

namespace Connected.Net.Rest.Formatters;

internal class JsonFormatter : Formatter
{
	public const string ContentType = "application/json";

	protected override async Task<IDictionary<string, object?>?> OnParseArguments()
	{
		if (Context is null)
			return null;

		return await Context.Request.Deserialize();
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

				await JsonSerializer.SerializeAsync(ms, content, new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase
				});

				ms.Seek(0, SeekOrigin.Begin);

				length = ms.Length;
				buffer = ms.ToArray();
			}

			Context.Response.ContentLength = length;

			if (length == 0)
				Context.Response.ContentType = "text/plain";
			else
				Context.Response.ContentType = ContentType;

			// Context.Response.StatusCode = StatusCodes.Status200OK;

			if (buffer is not null)
				await Context.Response.Body.WriteAsync(buffer);
		}

		await Context.Response.CompleteAsync();
	}
}
