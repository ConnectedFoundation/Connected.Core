using Connected.Authentication;
using Connected.Identities;
using Connected.Reflection;
using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace Connected.Net.Http;

public static class HttpExtensions
{
	private const string RequestArgumentsKey = "TP-REQUEST-ARGUMENTS";

	public static bool IsAjaxRequest(this HttpRequest? request)
	{
		if (request is null)
			return false;

		if (request.Headers is not null && request.Headers.TryGetValue("X-Requested-With", out StringValues value))
			return string.Equals(value, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

		return false;
	}

	public static async Task<IDictionary<string, object?>?> Deserialize(this HttpRequest request)
	{
		if (await ReadText(request) is not string text || string.IsNullOrWhiteSpace(text))
			return default;

		if (JsonNode.Parse(text, new JsonNodeOptions { PropertyNameCaseInsensitive = true }) is not JsonNode node)
			return default;

		var result = node.ToDictionary();

		request.HttpContext.SetRequestArguments(result);

		return result;
	}

	public static async Task<T?> Deserialize<T>(this HttpRequest request)
	{
		if (await ReadText(request) is not string text || string.IsNullOrWhiteSpace(text))
			return default;

		return await Serializer.Deserialize<T>(text);
	}

	private static async Task<string> ReadText(HttpRequest request)
	{
		using var reader = new StreamReader(request.Body, Encoding.UTF8);

		return await reader.ReadToEndAsync();
	}

	public static IDictionary<string, object?>? GetRequestArguments(this HttpContext context)
	{
		var result = context.Items[RequestArgumentsKey];

		return result is null ? null : (IDictionary<string, object?>)result;
	}

	public static void SetRequestArguments(this HttpContext context, IDictionary<string, object?> arguments)
	{
		if (context is null)
			return;

		context.Items[RequestArgumentsKey] = arguments;
	}

	public static async Task<TResult?> Get<TResult>(this IHttpService factory, string? requestUri, CancellationToken cancellationToken = default)
	{
		return await HandleResponse<TResult>(await factory.CreateClient().SendAsync(CreateGetMessage(requestUri), cancellationToken));
	}

	public static async Task<TResult?> Get<TResult>(this IHttpService factory, string? requestUri, object content, CancellationToken cancellationToken = default)
	{
		return await HandleResponse<TResult>(await factory.CreateClient().SendAsync(await CreateGetMessage(requestUri, content), cancellationToken));
	}

	public static async Task Post(this IHttpService factory, string? requestUri, object? content, CancellationToken cancellationToken = default)
	{
		await HandleResponse(await factory.CreateClient().SendAsync(await CreatePostMessage(requestUri, content), cancellationToken));
	}
	public static async Task<TResult?> Post<TResult>(this IHttpService factory, string? requestUri, object? content, CancellationToken cancellationToken = default)
	{
		var client = factory.CreateClient();
		var message = await CreatePostMessage(requestUri, content);
		var response = await client.SendAsync(message, cancellationToken);

		return await HandleResponse<TResult>(response);
	}
	private static HttpRequestMessage CreateGetMessage(string? requestUri)
	{
		return new HttpRequestMessage(HttpMethod.Get, requestUri);
	}

	private static async Task<HttpRequestMessage> CreateGetMessage(string? requestUri, object content)
	{
		return new HttpRequestMessage(HttpMethod.Get, requestUri)
		{
			Content = await CreateJsonContent(content)
		};
	}

	private static async Task<HttpRequestMessage> CreatePostMessage(string? requestUri, object? content)
	{
		return new HttpRequestMessage(HttpMethod.Post, requestUri)
		{
			Content = await CreateJsonContent(content)
		};
	}
	private static async Task HandleResponse(HttpResponseMessage response)
	{
		if (!response.IsSuccessStatusCode)
			await HandleResponseException(response);
	}

	private static async Task<T?> HandleResponse<T>(HttpResponseMessage response)
	{
		if (!response.IsSuccessStatusCode)
			await HandleResponseException(response);

		var content = response.Content.ReadAsStringAsync().Result;

		if (IsNull(content))
			return default;

		return await Serializer.Deserialize<T>(content);
	}

	private static async Task HandleResponseException(HttpResponseMessage response)
	{
		var ex = await ParseException(response.Content);

		if (ex is null)
			throw new WebException(response.ReasonPhrase);

		var source = string.Empty;
		var message = string.Empty;

		if (ex["source"] is JsonNode sourceNode)
			source = sourceNode.GetValue<string>();

		if (ex["message"] is JsonNode messageNode)
			message = messageNode.GetValue<string>();

		throw new WebException(message) { Source = source };
	}

	private static async Task<JsonNode?> ParseException(HttpContent responseContent)
	{
		if (responseContent is null)
			return default;

		try
		{
			var rt = responseContent.ReadAsStringAsync().Result;

			return await Serializer.Deserialize<JsonNode>(rt);
		}
		catch
		{
			return null;
		}
	}
	private static bool IsNull(string content)
	{
		return string.Equals(content, "null", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(content);
	}

	private static async Task<HttpContent?> CreateJsonContent(object? content)
	{
		if (content is null || Convert.IsDBNull(content))
			return new StringContent(string.Empty);

		if (await Serializer.Serialize(content) is not string c)
			return new StringContent(string.Empty, Encoding.UTF8, "application/json");

		return new StringContent(c, Encoding.UTF8, "application/json");
	}

	public static System.Security.Principal.IIdentity? ResolveIdentity(this IHttpContextAccessor context)
	{
		if (context.HttpContext?.User?.Identity is null)
			return null;

		if (!context.HttpContext.User.Identity.IsAuthenticated)
			return null;

		return context.HttpContext.User.Identity;
	}

	public static async Task<IIdentity?> ResolveIdentity(this HttpContext context)
	{
		if (context.User?.Identity is null)
			return null;

		if (!context.User.Identity.IsAuthenticated)
			return null;

		if (context.User.Identity is not IIdentityAccessor accessor || accessor.Identity?.Token is null)
			return null;

		using var scope = await Scope.Create().WithSystemIdentity();

		try
		{
			var identities = scope.ServiceProvider.GetRequiredService<IIdentityExtensions>();
			var result = await identities.Select(Dto.Factory.CreateValue(accessor.Identity.Token));

			await scope.Commit();

			return result;
		}
		catch
		{
			await scope.Rollback();

			return null;
		}
		finally
		{
			await scope.Flush();
		}
	}
}
