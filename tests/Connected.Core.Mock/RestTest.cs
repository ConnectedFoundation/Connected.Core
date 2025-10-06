using Connected.Core.Services.Mock;
using Connected.Entities;
using Connected.Services;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Connected.Core.Mock;
public abstract class RestTest(string serviceUrl)
{
	public const string InsertOperation = "insert";
	public const string UpdateOperation = "update";
	public const string DeleteOperation = "delete";
	public const string QueryOperation = "query";
	public const string SelectOperation = "select";
	public const string LookupOperation = "lookup";
	public const string PatchOperation = "patch";
	static RestTest()
	{
		Options = new JsonSerializerOptions()
		{
			PropertyNameCaseInsensitive = true
		};
	}

	private static JsonSerializerOptions Options { get; }

	protected string ServiceUrl { get; set; } = serviceUrl;

	protected async Task Post(string operation, object? instance)
	{
		using var client = PrepareClient();

		await HandleResponse(await client.PostAsync(TestConfiguration.ParseUrl(ServiceUrl, operation), CreateContent(instance)));
	}

	protected async Task<T?> Post<T>(string operation, object? instance)
	{
		using var client = PrepareClient();

		return await HandleResponse<T>(await client.PostAsync(TestConfiguration.ParseUrl(ServiceUrl, operation), CreateContent(instance)));
	}

	protected async Task Put(string operation, object? instance)
	{
		using var client = PrepareClient();

		await HandleResponse(await client.PutAsync(TestConfiguration.ParseUrl(ServiceUrl, operation), CreateContent(instance)));
	}

	protected async Task Delete(string operation, Dictionary<string, object?>? properties)
	{
		using var client = PrepareClient();
		var url = TestConfiguration.ParseUrl(ServiceUrl, operation);

		if (properties is not null && properties.Count > 0)
			url = $"{TestConfiguration.ParseUrl(ServiceUrl, operation)}{CreateQueryString(properties)}";

		await HandleResponse(await client.DeleteAsync(url));
	}

	protected async Task Patch<T>(string operation, PatchDtoMock<T> dto)
		where T : notnull
	{
		using var client = PrepareClient();

		await HandleResponse(await client.PatchAsync(TestConfiguration.ParseUrl(ServiceUrl, operation), CreateContent(dto)));
	}
	protected async Task<T?> Put<T>(string operation, object? instance)
	{
		using var client = PrepareClient();

		return await HandleResponse<T>(await client.PutAsync(TestConfiguration.ParseUrl(ServiceUrl, operation), CreateContent(instance)));
	}

	protected async Task<T?> Get<T>(string operation, Dictionary<string, object?>? properties)
	{
		using var client = PrepareClient();
		var url = TestConfiguration.ParseUrl(ServiceUrl, operation);

		if (properties is not null && properties.Count > 0)
			url = $"{TestConfiguration.ParseUrl(ServiceUrl, operation)}{CreateQueryString(properties)}";

		return await HandleResponse<T>(await client.GetAsync(url));
	}

	protected async Task<T?> Get<T>(string operation, List<KeyValuePair<string, object?>>? properties)
	{
		using var client = PrepareClient();
		var url = TestConfiguration.ParseUrl(ServiceUrl, operation);

		if (properties is not null && properties.Count > 0)
			url = $"{TestConfiguration.ParseUrl(ServiceUrl, operation)}{CreateQueryString(properties)}";

		return await HandleResponse<T>(await client.GetAsync(url));
	}

	protected async Task Upload(string operation, string directory, string fileName)
	{
		using var client = PrepareClient();
		var raw = new ByteArrayContent(File.ReadAllBytes(fileName));

		raw.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
		raw.Headers.Add("directory", directory);

		var content = new MultipartFormDataContent
		{
			{ raw, Path.GetFileName(fileName), fileName }
		};

		await HandleResponse(await client.PostAsync(TestConfiguration.ParseUrl(ServiceUrl, operation), content));
	}

	private static async Task<T?> HandleResponse<T>(HttpResponseMessage response)
	{
		if (!response.IsSuccessStatusCode)
			throw new Exception(await response.Content.ReadAsStringAsync());

		var responseContent = await response.Content.ReadAsStringAsync();

		if (IsNull(responseContent))
			return default;

		return JsonSerializer.Deserialize<T>(responseContent, Options);
	}

	private static async Task HandleResponse(HttpResponseMessage response)
	{
		if (!response.IsSuccessStatusCode)
			throw new Exception(await response.Content.ReadAsStringAsync());
	}

	private static HttpClient PrepareClient()
	{
		var client = new HttpClient();

		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", TestConfiguration.Authentication);

		return client;
	}

	private static StringContent CreateContent(object? instance)
	{
		return new StringContent(instance is null ? string.Empty : JsonSerializer.Serialize(instance), Encoding.UTF8, "application/json");
	}

	private static QueryString CreateQueryString(Dictionary<string, object?>? properties)
	{
		var result = new QueryString();

		if (properties is not null)
		{
			foreach (var property in properties)
				result = result.Add(property.Key, property.Value?.ToString() ?? string.Empty);
		}

		return result;
	}

	private static QueryString CreateQueryString(List<KeyValuePair<string, object?>>? properties)
	{
		var result = new QueryString();

		if (properties is not null)
		{
			foreach (var property in properties)
				result = result.Add(property.Key, property.Value?.ToString() ?? string.Empty);
		}

		return result;
	}

	private static bool IsNull(string content)
	{
		return string.Equals(content, "null", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(content);
	}

	protected async Task<TReturnValue?> Insert<TDto, TReturnValue>(TDto dto)
		where TDto : IDto
	{
		return await Post<TReturnValue>(InsertOperation, dto);
	}

	protected async Task Update<TDto>(TDto dto)
		where TDto : IDto
	{
		await Put(UpdateOperation, dto);
	}

	protected async Task<List<TEntity>> Query<TEntity>(Dictionary<string, object?> properties)
	{
		return await Get<List<TEntity>>(QueryOperation, properties) ?? new List<TEntity>();
	}

	protected async Task<TEntity?> Select<TEntity>(Dictionary<string, object?> properties)
	{
		return await Get<TEntity?>(SelectOperation, properties);
	}

	protected async Task<TEntity?> Select<TEntity>(object id)
	{
		return await Get<TEntity?>(SelectOperation, new Dictionary<string, object?>
		{
			{ "id", id }
		});
	}

	protected async Task<List<TEntity>> Query<TEntity>()
		where TEntity : IEntity
	{
		return await Get<List<TEntity>>(QueryOperation, new Dictionary<string, object?>()) ?? new List<TEntity>();
	}

	protected async Task Delete<TPrimaryKey>(TPrimaryKey id)
	{
		await Delete(DeleteOperation, new Dictionary<string, object?>
		{
			{ "id", id }
		});
	}
}