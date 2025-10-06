using Connected.Core.Entities.Mock;
using Connected.Core.Services.Mock;
using Microsoft.AspNetCore.Http;
using System.Collections;
using System.Net.Http.Headers;
using System.Reflection;
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

	protected async Task Post<TDto>(string operation, TDto? dto)
		where TDto : DtoMock
	{
		using var client = PrepareClient();

		await HandleResponse(await client.PostAsync(TestConfiguration.ParseUrl(ServiceUrl, operation), CreateContent(dto)));
	}

	protected async Task<TReturnValue?> Post<TDto, TReturnValue>(string operation, TDto? dto)
		where TDto : DtoMock
	{
		using var client = PrepareClient();

		return await HandleResponse<TReturnValue>(await client.PostAsync(TestConfiguration.ParseUrl(ServiceUrl, operation), CreateContent(dto)));
	}

	protected async Task Put<TDto>(string operation, TDto? dto)
		where TDto : DtoMock
	{
		using var client = PrepareClient();

		await HandleResponse(await client.PutAsync(TestConfiguration.ParseUrl(ServiceUrl, operation), CreateContent(dto)));
	}

	protected async Task Delete<TDto>(string operation, TDto? dto)
		where TDto : DtoMock
	{
		using var client = PrepareClient();
		var url = $"{TestConfiguration.ParseUrl(ServiceUrl, operation)}{CreateQueryString(dto)}";

		await HandleResponse(await client.DeleteAsync(url));
	}

	protected async Task Patch<TPrimaryKey>(string operation, PatchDtoMock<TPrimaryKey> dto)
		where TPrimaryKey : notnull
	{
		using var client = PrepareClient();

		await HandleResponse(await client.PatchAsync(TestConfiguration.ParseUrl(ServiceUrl, operation), CreateContent(dto)));
	}

	protected async Task<TEntity?> Put<TDto, TEntity>(string operation, TDto? dto)
		where TEntity : EntityMock
		where TDto : DtoMock
	{
		using var client = PrepareClient();

		return await HandleResponse<TEntity>(await client.PutAsync(TestConfiguration.ParseUrl(ServiceUrl, operation), CreateContent(dto)));
	}

	protected async Task<List<TEntity>> GetList<TDto, TEntity>(string operation, TDto? dto)
		where TEntity : EntityMock
		where TDto : DtoMock
	{
		using var client = PrepareClient();
		var url = $"{TestConfiguration.ParseUrl(ServiceUrl, operation)}{CreateQueryString(dto)}";

		return await HandleResponse<List<TEntity>>(await client.GetAsync(url)) ?? [];
	}

	protected async Task<TEntity?> Get<TDto, TEntity>(string operation, TDto? dto)
		where TEntity : EntityMock
		where TDto : DtoMock
	{
		using var client = PrepareClient();
		var url = $"{TestConfiguration.ParseUrl(ServiceUrl, operation)}{CreateQueryString(dto)}";

		return await HandleResponse<TEntity>(await client.GetAsync(url));
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

	private static QueryString CreateQueryString<TDto>(TDto? dto)
		where TDto : DtoMock
	{
		if (dto is null)
			return new QueryString();

		var result = new QueryString();
		var properties = dto.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);

		foreach (var property in properties)
		{
			var value = property.GetValue(dto, null);

			if (value is null)
				result = result.Add(property.Name, string.Empty);
			else if (value is IEnumerable enumerable && value is not string)
			{
				var enumerator = enumerable.GetEnumerator();

				while (enumerator.MoveNext())
				{
					var itemValue = enumerator.Current?.ToString() ?? string.Empty;

					result = result.Add(property.Name, itemValue);
				}
			}
			else
				result = result.Add(property.Name, value.ToString() ?? string.Empty);
		}

		return result;
	}

	private static bool IsNull(string content)
	{
		return string.Equals(content, "null", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(content);
	}

	protected async Task<TReturnValue?> Insert<TDto, TReturnValue>(TDto dto)
		where TDto : DtoMock
	{
		return await Post<TDto, TReturnValue>(InsertOperation, dto);
	}

	protected async Task Update<TDto>(TDto dto)
		where TDto : DtoMock
	{
		await Put(UpdateOperation, dto);
	}

	protected async Task<List<TEntity>> Query<TDto, TEntity>(TDto? dto)
		where TDto : DtoMock
		where TEntity : EntityMock
	{
		return await GetList<TDto, TEntity>(QueryOperation, dto);
	}

	protected async Task<TEntity?> Select<TDto, TEntity>(TDto? dto)
		where TDto : DtoMock
		where TEntity : EntityMock
	{
		return await Get<TDto, TEntity>(SelectOperation, dto);
	}

	protected async Task Delete<TDto>(TDto dto)
		where TDto : DtoMock
	{
		await Delete(DeleteOperation, dto);
	}
}