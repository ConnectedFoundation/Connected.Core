using Connected.Annotations;
using Connected.Net.Rest;
using Connected.Net.Rest.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Immutable;

namespace Connected.Net;
public static class NetExtensions
{
	private static bool Enabled { get; set; }

	private static readonly string[] configurePolicy = ["http://localhost"];

	public static void AddCors(this IHostApplicationBuilder builder)
	{
		var provider = builder.Services.BuildServiceProvider(true);

		using var scope = provider.CreateAsyncScope();
		var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
		var cors = new CorsConfiguration();

		configuration.GetSection("cors")?.Bind(cors);

		if (cors.Enabled)
		{
			builder.Services.AddCors(options => options.AddPolicy("Connected",
				 builder =>
				 {
					 var origin = configurePolicy;

					 if (!string.IsNullOrWhiteSpace(cors.Origins))
						 origin = cors.Origins.Split([','], StringSplitOptions.RemoveEmptyEntries);

					 builder.AllowAnyMethod()
						  .AllowAnyHeader()
						  .WithOrigins(origin)
						  .AllowCredentials();
				 }));
		}

	}

	public static void AddHttpServices(this IHostApplicationBuilder builder)
	{
		builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
		builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
		builder.Services.AddHttpClient();
		builder.Services.AddResponseCompression();
		builder.Services.AddResponseCaching();
	}

	public static void ActivateCors(this IApplicationBuilder app)
	{
		if (Enabled)
			app.UseCors("Connected");
	}

	public static void ActivateHttpServices(this IApplicationBuilder app)
	{
		app.UseResponseCompression();
		app.UseResponseCaching();
	}

	public static void ActivateRest(this IApplicationBuilder builder)
	{
		if (builder.ApplicationServices.GetService<IResolutionService>() is not IResolutionService resolution)
			return;

		if (resolution.QueryRoutes() is not ImmutableList<Tuple<string, ServiceOperationVerbs>> routes || !routes.Any())
			return;

		builder.UseEndpoints(config =>
		{
			foreach (var route in routes)
			{
				config.Map(route.Item1, async (httpContext) =>
				{
					using var handler = new ServiceRequestDelegate(httpContext);

					await handler.Invoke();
				});

				config.Map($"{route.Item1}/dto", async (httpContext) =>
				{
					using var handler = new DtoRequestDelegate(httpContext);

					await handler.Invoke();
				});
			}
		});
	}
}
