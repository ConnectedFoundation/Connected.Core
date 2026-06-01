using Connected.Authentication;
using Connected.Globalization.Languages;
using Connected.Services;
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

		builder.Services.AddOptions<RequestLocalizationOptions>()
			.Configure(async (o) =>
			{
				using var scope = await Scope.Create().WithSystemIdentity();

				o.DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture);
				o.FallBackToParentCultures = true;
				o.FallBackToParentUICultures = true;
				o.RequestCultureProviders.Insert(2, new DefaultSettingsCultureProvider());
				o.RequestCultureProviders.Insert(2, new DomainCultureProvider());
				o.RequestCultureProviders.Insert(1, new IdentityCultureProvider());

				var instances = o.RequestCultureProviders.Where(f => f.GetType() == typeof(AcceptLanguageHeaderRequestCultureProvider)).ToList();

				instances.ForEach(obj => o.RequestCultureProviders.Remove(obj));

				var languages = scope.ServiceProvider.GetRequiredService<ILanguageService>();
				var items = await languages.Query(null);

				o.SupportedCultures ??= [];
				o.SupportedUICultures ??= [];

				foreach (var language in items)
				{
					if (string.IsNullOrWhiteSpace(language.Culture))
						continue;

					o.SupportedCultures.Add(new CultureInfo(language.Culture));
					o.SupportedUICultures.Add(new CultureInfo(language.Culture));
				}
			});
	}

	public static async Task ActivateLocalization(this IApplicationBuilder app)
	{
		var service = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();

		if (service is not null)
			app.UseRequestLocalization(service.Value);
	}
}
