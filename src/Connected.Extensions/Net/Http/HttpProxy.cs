using Connected.Reflection;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Connected.Net.Http;

public abstract class HttpProxy(IHttpService http)
{
	public const string LegacyServerSettingName = "legacyServer";
	public const string LegacyServerSysToken = "sysToken";

	protected IHttpService Http { get; } = http;

	private async Task<HttpClient> CreateClient()
	{
		await OnValidate();

		var client = Http.CreateClient();

		await OnCreateClient(client);

		return client;
	}

	protected virtual Task OnValidate()
	{
		return Task.CompletedTask;
	}

	protected virtual Task OnCreateClient(HttpClient client)
	{
		return Task.CompletedTask;
	}

	protected async Task Post(string url, object? body, CancellationToken cancel = default)
	{
		using var client = await CreateClient();

		var content = await CreateContent(body);
		var response = await client.PostAsync(url, content, cancel);

		await HandleResponse(response, cancel);
	}

	protected async Task<TResult?> Post<TResult>(string url, object? body, CancellationToken cancel = default)
	{
		using var client = await CreateClient();

		var content = await CreateContent(body);
		var response = await client.PostAsync(url, content, cancel);

		return await HandleResponse<TResult>(response, cancel);
	}

	private static async Task HandleResponse(HttpResponseMessage response, CancellationToken cancel = default)
	{
		if (!response.IsSuccessStatusCode)
			await HandleResponseException(response, cancel);
	}

	private static async Task<T?> HandleResponse<T>(HttpResponseMessage response, CancellationToken cancel = default)
	{
		if (!response.IsSuccessStatusCode)
			await HandleResponseException(response, cancel);

		var content = await response.Content.ReadAsStringAsync(cancel);

		if (IsNull(content))
			return default;

		return await Serializer.Deserialize<T>(content);
	}

	private static async Task HandleResponseException(HttpResponseMessage response, CancellationToken cancel = default)
	{
		if (await ParseException(response.Content, cancel) is not JsonDocument exception)
			throw new Exception(response.ReasonPhrase);

		var source = string.Empty;
		var message = string.Empty;

		if (exception.RootElement.TryGetProperty("source", out JsonElement sourceElement))
			source = sourceElement.GetString();

		if (exception.RootElement.TryGetProperty("message", out JsonElement messageElement))
			message = messageElement.GetString();

		throw new HttpException(HttpStatusCode.InternalServerError, source, message);
	}

	private static async Task<JsonDocument?> ParseException(HttpContent responseContent, CancellationToken cancel = default)
	{
		if (responseContent is null)
			return null;

		try
		{
			var text = await responseContent.ReadAsStringAsync(cancel);

			return await Serializer.Deserialize<JsonDocument>(text);
		}
		catch
		{
			return null;
		}
	}

	private static bool IsNull(string? content)
	{
		return string.Equals(content, "null", StringComparison.OrdinalIgnoreCase)
			 || string.IsNullOrWhiteSpace(content);
	}

	private static async Task<HttpContent?> CreateContent(object? content)
	{
		if (content is null)
			return null;

		var c = await Serializer.Serialize(content);

		if (c is null)
			return null;

		var sc = new StringContent(c, Encoding.UTF8, "application/json");

		sc.Headers.Add("Content-Encoding", "gzip");

		return sc;
	}
}