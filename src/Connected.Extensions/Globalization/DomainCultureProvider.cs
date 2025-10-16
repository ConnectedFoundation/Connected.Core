using Connected.Authentication;
using Connected.Globalization.Languages;
using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Connected.Globalization;
internal sealed class DomainCultureProvider : CultureProviderBase, IRequestCultureProvider
{
	public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
	{
		var domain = httpContext.Request.Host.ToString().Split('.').LastOrDefault()?.Split(':').FirstOrDefault();

		if (string.IsNullOrWhiteSpace(domain))
			return await Unresolved;

		using var scope = await Scope.Create().WithSystemIdentity();

		try
		{
			var languageService = scope.ServiceProvider.GetRequiredService<ILanguageService>();
			var languageMappingService = scope.ServiceProvider.GetRequiredService<ILanguageMappingService>();

			var language = await languageService.Match(languageMappingService, domain);

			if (language is null)
				return await Unresolved;

			var culture = CultureInfo.GetCultureInfo(language.Lcid);

			if (culture is null)
				return await Unresolved;

			return new ProviderCultureResult(culture.Name, culture.Name);
		}
		finally
		{
			await scope.Commit();
		}
	}
}