using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Connected.Globalization;
public static class LocalizationExtensions
{
	public static void AddLocalization(this IHostApplicationBuilder builder)
	{
		builder.Services.AddScoped<RequestLocalizationCookiesMiddleware>();

		builder.Services.Configure<RequestLocalizationOptions>(o =>
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

	public static void ActivateLocalization(this IApplicationBuilder app)
	{
		var service = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();

		if (service is not null)
			app.UseRequestLocalization(service.Value);
	}
}
