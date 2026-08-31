using Connected.Configuration;
using Connected.Net.Rest.OpenApi.Configuration;
using Connected.Net.Rest.OpenApi.Documentation;
using Connected.Net.Rest.OpenApi.Generation;
using Connected.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Writers;
using Scalar.AspNetCore;

namespace Connected.Net.Rest.OpenApi;

/// <summary>
/// Registers the <c>/openapi/v1.json</c> document endpoint and the Scalar API reference UI
/// at <c>/api</c>. Picked up automatically once this assembly is listed as a "dependencies"
/// entry in configuration, the same opt-in mechanism used by the other optional
/// Connected.Core packages (e.g. the storage providers).
/// </summary>
public sealed class OpenApiStartup : Connected.Runtime.Startup
{
	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.Configure<OpenApiOptions>(Configuration.GetSection(OpenApiOptions.Path));

		// Caches parsed XML doc files per assembly for the life of the process.
		services.AddSingleton<XmlDocumentationProvider>();
	}

	protected override void OnConfigure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		app.UseEndpoints(endpoints =>
		{
			endpoints.MapGet("/openapi/v1.json", async context =>
			{
				var runtimeService = context.RequestServices.GetRequiredService<IRuntimeService>();
				var configuration = context.RequestServices.GetRequiredService<IConfigurationService>();
				var options = context.RequestServices.GetRequiredService<IOptionsMonitor<OpenApiOptions>>();
				var documentation = context.RequestServices.GetRequiredService<XmlDocumentationProvider>();
				var document = await new OpenApiDocumentGenerator(runtimeService, configuration, options, documentation).Generate();

				context.Response.ContentType = "application/json";

				using var writer = new StringWriter();

				document.SerializeAsV3(new OpenApiJsonWriter(writer));

				await context.Response.WriteAsync(writer.ToString());
			});

			endpoints.MapScalarApiReference("/api", options =>
			{
				options.OpenApiRoutePattern = "/openapi/v1.json";
				options.Title = "Connected API Reference";

				/*
				 * Kept base64-encoded, not decoded: BearerAuthenticationProvider.ParseToken() decodes
				 * whatever arrives in the Authorization header itself, so the wire value (and therefore
				 * what Scalar sends) must be the encoded form straight out of configuration. No fallback
				 * to identities:maintenance: this endpoint may be publicly reachable, so the token is only
				 * ever prefilled when explicitly opted into via openApi:bearerToken.
				 */
				var bearerToken = app.ApplicationServices.GetRequiredService<IOptionsMonitor<OpenApiOptions>>().CurrentValue.BearerToken;

				if (!string.IsNullOrWhiteSpace(bearerToken))
				{
					options.Authentication = new ScalarAuthenticationOptions
					{
						PreferredSecurityScheme = OpenApiDocumentGenerator.BearerSecurityScheme,
						Http = new HttpOptions
						{
							Bearer = new HttpBearerOptions { Token = bearerToken }
						}
					};
				}
			});
		});
	}
}
