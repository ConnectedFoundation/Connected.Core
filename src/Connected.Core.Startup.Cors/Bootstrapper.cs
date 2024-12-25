using Connected.Annotations;
using Connected.Configuration.Settings;
using Connected.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Startup.Cors;

[Priority(49)]
internal sealed class Bootstrapper : Runtime.Startup
{
	public static IServiceProvider Services { get; private set; } = default!;
	private bool Enabled { get; set; }
	protected override void OnConfigureServices(IServiceCollection services)
	{
		var provider = services.BuildServiceProvider(true);

		using var scope = provider.CreateAsyncScope();

		var settings = scope.ServiceProvider.GetRequiredService<ISettingService>();

		var cors = settings.Select((NameDto)"Cors Enabled").Result;

		if (cors is null)
			return;

		Enabled = Convert.ToBoolean(cors.Value);

		if (Enabled)
		{
			services.AddCors(options => options.AddPolicy("Connected",
				 builder =>
				 {
					 var origins = settings.Select((NameDto)"Cors Origins").Result;
					 var origin = new string[] { "http://localhost" };

					 if (!string.IsNullOrWhiteSpace(origins?.Value))
						 origin = origins.Value.Split([","], StringSplitOptions.RemoveEmptyEntries);

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