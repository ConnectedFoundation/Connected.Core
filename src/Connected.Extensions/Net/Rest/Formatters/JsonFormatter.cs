using Connected.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Connected.Net.Rest.Formatters;

internal class JsonFormatter : Formatter
{
	public const string ContentType = "application/json";
	private static readonly JsonSerializerOptions _options;

	static JsonFormatter()
	{
		_options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
	}


	protected override async Task<IDictionary<string, object?>?> OnParseArguments()
	{
		if (Context is null)
			return null;

		using var reader = new StreamReader(Context.Request.Body, Encoding.UTF8);
		var text = await reader.ReadToEndAsync();

		if (JsonNode.Parse(text, new JsonNodeOptions { PropertyNameCaseInsensitive = true }) is not JsonNode node)
			return default;

		return WithRouteValues(node.ToDictionary());
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

			// Context.Response.StatusCode = StatusCodes.Status200OK;

			if (buffer is not null)
				await Context.Response.Body.WriteAsync(buffer);
		}

		await Context.Response.CompleteAsync();
	}

	protected override async Task OnRenderError(Exception ex)
	{
		if (Context is null)
			return;

		if (!Context.Response.HasStarted)
		{
			using var ms = new MemoryStream();
			using var writer = new Utf8JsonWriter(ms);

			writer.WriteStartObject();

			writer.WriteString("message", ex.Message);

			if (IsDevelopment())
			{
				writer.WriteString("source", ex.Source);
				writer.WriteString("stackTrace", ex.StackTrace);
			}

			writer.WriteEndObject();

			await writer.FlushAsync();

			ms.Seek(0, SeekOrigin.Begin);

			Context.Response.ContentLength = ms.Length;
			Context.Response.ContentType = "application/json";

			await Context.Response.Body.WriteAsync(ms.ToArray());
		}

		await Context.Response.CompleteAsync();
	}

	private bool IsDevelopment()
	{
		if (Scope is null)
			return false;

		return Scope.Value.ServiceProvider.GetService<IHostEnvironment>() is IHostEnvironment env && env.IsDevelopment();
	}
}
