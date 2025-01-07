using Connected.Reflection;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Connected.Net.Http;

public abstract class HttpProxy
{
	public const string LegacyServerSettingName = "legacyServer";
	public const string LegacyServerSysToken = "sysToken";

	protected HttpProxy(IHttpService http)
	{
		Http = http;
	}

	protected IHttpService Http { get; }

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

	private async Task HandleResponse(HttpResponseMessage response, CancellationToken cancel = default)
	{
		if (!response.IsSuccessStatusCode)
			await HandleResponseException(response, cancel);
	}

	private async Task<T?> HandleResponse<T>(HttpResponseMessage response, CancellationToken cancel = default)
	{
		if (!response.IsSuccessStatusCode)
			await HandleResponseException(response, cancel);

		var content = await response.Content.ReadAsStringAsync(cancel);

		if (IsNull(content))
			return default;

		return await Serializer.Deserialize<T>(content);
	}

	private async Task HandleResponseException(HttpResponseMessage response, CancellationToken cancel = default)
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

	private async Task<HttpContent?> CreateContent(object? content)
	{
		if (content is null)
			return null;

		var c = await Serializer.Serialize(content);

		if (c is null)
			return null;

		content = CompressString(c);

		var sc = new StringContent(c, Encoding.UTF8, "application/json");

		sc.Headers.Add("Content-Encoding", "gzip");

		return sc;
	}

	private string CompressString(string text)
	{
		var buffer = Encoding.UTF8.GetBytes(text);

		using var ms = new MemoryStream();
		using var zip = new GZipStream(ms, CompressionMode.Compress, true);

		zip.Write(buffer, 0, buffer.Length);

		ms.Position = 0;

		var compressedData = new byte[ms.Length];

		ms.Read(compressedData, 0, compressedData.Length);

		var zipBuffer = new byte[compressedData.Length + 4];

		Buffer.BlockCopy(compressedData, 0, zipBuffer, 4, compressedData.Length);
		Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, zipBuffer, 0, 4);

		return Convert.ToBase64String(zipBuffer);
	}
}