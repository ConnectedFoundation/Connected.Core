using Connected.Annotations;
using Connected.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Connected.Startup.StaticFiles;

[Priority(56)]
public sealed class StaticFilesStartup : Runtime.Startup
{
	public static IServiceProvider Services { get; private set; } = default!;

	protected override void OnConfigure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		var cachePeriod = env.IsDevelopment() ? "600" : "604800";
		var contentTypeProvider = new FileExtensionContentTypeProvider();

		var scope = app.ApplicationServices.CreateScope();
		var rt = scope.ServiceProvider.GetRequiredService<IRuntimeService>();

		foreach (var startup in rt.QueryStartups().Result)
		{
			try
			{
				var fileProvider = new ManifestEmbeddedFileProvider(startup.GetType().Assembly, "wwwroot");
				var options = new StaticFileOptions
				{
					FileProvider = fileProvider,
					//ContentTypeProvider = contentTypeProvider,
					RequestPath = string.Empty
				};

				app.UseStaticFiles(options);
			}
			catch { }
		}

		var staticOptions = new StaticFileOptions
		{
			ContentTypeProvider = contentTypeProvider,
		};

		app.UseStaticFiles(staticOptions);
		app.UseResponseCompression();
	}
}