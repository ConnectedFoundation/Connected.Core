using Connected.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Startup.Cors;

[Priority(49)]
public sealed class CorsStartup : Runtime.Startup
{
	public static IServiceProvider Services { get; private set; } = default!;
	private bool Enabled { get; set; }

	private static readonly string[] configurePolicy = ["http://localhost"];

	protected override void OnConfigureServices(IServiceCollection services)
	{
		var provider = services.BuildServiceProvider(true);

		using var scope = provider.CreateAsyncScope();
		var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
		var cors = new CorsConfiguration();

		configuration.GetSection("cors")?.Bind(cors);

		if (cors.Enabled)
		{
			services.AddCors(options => options.AddPolicy("Connected",
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

	protected override void OnConfigure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		if (Enabled)
			app.UseCors("Connected");
	}
}