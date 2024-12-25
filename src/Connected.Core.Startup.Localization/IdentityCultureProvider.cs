using Connected.Authentication;
using Connected.Globalization.Languages;
using Connected.Identities.Globalization;
using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Connected.Startup.Localization;
internal sealed class IdentityCultureProvider : CultureProviderBase
{
	public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
	{
		using var scope = Scope.Create();

		var authenticationService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
		var identityGlobalizationService = scope.ServiceProvider.GetRequiredService<IIdentityGlobalizationService>();
		var languageService = scope.ServiceProvider.GetRequiredService<ILanguageService>();
		var identity = await authenticationService.SelectIdentity();

		if (identity is null)
			return await Unresolved;

		var globalization = await identityGlobalizationService.Select(new PrimaryKeyDto<string> { Id = identity.Token });

		if (globalization is null || globalization.Language is null)
			return await Unresolved;

		var language = await languageService.Select(new PrimaryKeyDto<int> { Id = globalization.Language.GetValueOrDefault() });

		if (language is null || language.Status == Entities.Status.Disabled)
			return await Unresolved;

		var culture = CultureInfo.GetCultureInfo(language.Lcid);

		if (culture is null)
			return await Unresolved;

		return new ProviderCultureResult(culture.Name, culture.Name);
	}
}
