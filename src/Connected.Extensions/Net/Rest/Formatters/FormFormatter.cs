using Connected.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Reflection;
using System.Text;

namespace Connected.Net.Rest.Formatters;

internal class FormFormatter : Formatter
{
	public const string ContentType = "application/x-www-form-urlencoded";

	protected override async Task<IDictionary<string, object?>?> OnParseArguments()
	{
		if (Context is null)
			return null;

		using var reader = new StreamReader(Context.Request.Body, Encoding.UTF8);
		var body = await reader.ReadToEndAsync();
		var qs = QueryHelpers.ParseNullableQuery(body);

		if (qs is null)
			return null;

		var result = new Dictionary<string, object?>();

		foreach (var q in qs)
			result.Add(q.Key, q.Value);

		return result;
	}

	protected override async Task OnRenderResult(object? content)
	{
		if (Context is null)
			return;

		if (!Context.Response.HasStarted)
		{
			var qs = new QueryString();

			if (content is not null)
			{
				foreach (var property in content.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
				{
					if (!property.PropertyType.IsTypePrimitive())
						continue;

					var value = property.GetValue(content) as string;

					if (value is not null)
						qs.Add(property.Name.ToCamelCase(), value);
				}
			}

			var buffer = Encoding.UTF8.GetBytes(qs.ToUriComponent());

			Context.Response.ContentLength = buffer.Length;
			Context.Response.ContentType = ContentType;
			Context.Response.StatusCode = StatusCodes.Status200OK;

			await Context.Response.Body.WriteAsync(buffer);
		}

		await Context.Response.CompleteAsync();
	}
}
