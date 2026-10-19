using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace Connected.Net.Rest.Tests;

/// <summary>
/// Holds what a middleware that renders an exception can do through its context: set the status, write any body,
/// and know whether it is development.
/// </summary>
[TestClass]
public sealed class ExceptionRenderContextTests
{
	private sealed class Environment(string name)
		: IHostEnvironment
	{
		public string EnvironmentName { get; set; } = name;
		public string ApplicationName { get; set; } = "test";
		public string ContentRootPath { get; set; } = string.Empty;
		public IFileProvider ContentRootFileProvider { get; set; } = null!;
	}

	/// <summary>
	/// A response that has been sent already, as one is once its first bytes are on the wire.
	/// </summary>
	private sealed class StartedResponse
		: HttpResponseFeature
	{
		public override bool HasStarted => true;
	}

	private static (ExceptionRenderContext Context, DefaultHttpContext Http) Create(string? environment = null)
	{
		var services = new ServiceCollection();

		if (environment is not null)
			services.AddSingleton<IHostEnvironment>(new Environment(environment));

		var http = new DefaultHttpContext
		{
			RequestServices = services.BuildServiceProvider()
		};

		http.Response.Body = new MemoryStream();

		return (new ExceptionRenderContext(new InvalidOperationException("boom"), http), http);
	}

	private static string Body(DefaultHttpContext http)
	{
		http.Response.Body.Position = 0;

		return Encoding.UTF8.GetString(((MemoryStream)http.Response.Body).ToArray());
	}

	[TestMethod]
	public async Task The_status_and_the_body_are_written_as_json()
	{
		var (context, http) = Create();

		await context.Write(StatusCodes.Status409Conflict, new { errorCode = "E-001", message = "Taken." });

		Assert.AreEqual(409, http.Response.StatusCode);
		Assert.AreEqual("application/json", http.Response.ContentType);
		Assert.AreEqual("""{"errorCode":"E-001","message":"Taken."}""", Body(http));
	}

	[TestMethod]
	public async Task The_properties_of_an_object_are_written_in_camel_case()
	{
		var (context, http) = Create();

		await context.Write(400, new { ErrorCode = "X", Args = new { Limit = 3 } });

		Assert.AreEqual("""{"errorCode":"X","args":{"limit":3}}""", Body(http));
	}

	[TestMethod]
	public async Task The_keys_of_a_dictionary_are_written_as_they_are_given()
	{
		var (context, http) = Create();

		await context.Write(400, new Dictionary<string, object?> { ["ErrorCode"] = "X", ["args"] = new { Limit = 3 } });

		Assert.AreEqual("""{"ErrorCode":"X","args":{"limit":3}}""", Body(http));
	}

	[TestMethod]
	public async Task The_length_of_the_body_is_declared()
	{
		var (context, http) = Create();

		await context.Write(400, new { a = 1 });

		Assert.AreEqual(Encoding.UTF8.GetByteCount("""{"a":1}"""), http.Response.ContentLength);
	}

	[TestMethod]
	public async Task A_response_that_has_started_is_left_alone()
	{
		var (context, http) = Create();

		http.Features.Set<IHttpResponseFeature>(new StartedResponse { Body = http.Response.Body });

		await context.Write(409, new { a = 1 });

		Assert.AreEqual(200, http.Response.StatusCode);
		Assert.AreEqual(string.Empty, Body(http));
	}

	[TestMethod]
	public void The_exception_is_the_one_the_request_failed_with()
	{
		var (context, _) = Create();

		Assert.AreEqual("boom", context.Exception.Message);
	}

	[TestMethod]
	public void It_is_development_only_in_the_development_environment()
	{
		Assert.IsTrue(Create(Environments.Development).Context.IsDevelopment);
		Assert.IsFalse(Create(Environments.Production).Context.IsDevelopment);
		Assert.IsFalse(Create().Context.IsDevelopment);
	}
}
