using Connected.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Connected.Startup.Localization;

[Priority(50)]
public sealed class LocalizationStartup : Runtime.Startup
{
	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.AddScoped<RequestLocalizationCookiesMiddleware>();

		services.Configure<RequestLocalizationOptions>(o =>
		{
			o.DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture);
			o.FallBackToParentCultures = true;
			o.FallBackToParentUICultures = true;

			o.RequestCultureProviders.Insert(2, new DefaultSettingsCultureProvider());
			o.RequestCultureProviders.Insert(2, new DomainCultureProvider());
			o.RequestCultureProviders.Insert(1, new IdentityCultureProvider());

			var instances = o.RequestCultureProviders.Where(f => f.GetType() == typeof(AcceptLanguageHeaderRequestCultureProvider)).ToList();

			instances.ForEach(obj => o.RequestCultureProviders.Remove(obj));
		});
	}

	protected override void OnConfigure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		var service = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();

		if (service is not null)
			app.UseRequestLocalization(service.Value);
	}
}