using Connected.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Hosting;

namespace Connected.Startup.StaticFiles;

[Priority(56)]
internal sealed class Bootstrapper : Runtime.Startup
{
	public static IServiceProvider Services { get; private set; } = default!;

	protected override void OnConfigure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		var cachePeriod = env.IsDevelopment() ? "600" : "604800";
		var contentTypeProvider = new FileExtensionContentTypeProvider();

		contentTypeProvider.Mappings[".webmanifest"] = "application/manifest+json";

		var staticOptions = new StaticFileOptions
		{
			OnPrepareResponse = ctx =>
			{
				ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={cachePeriod}");
			},
			ContentTypeProvider = contentTypeProvider,
		};

		app.UseStaticFiles(staticOptions);
		app.UseResponseCompression();
	}
}