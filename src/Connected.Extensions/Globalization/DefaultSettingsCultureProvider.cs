using Connected.Authentication;
using Connected.Configuration.Settings;
using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Globalization;
internal sealed class DefaultSettingsCultureProvider : CultureProviderBase, IRequestCultureProvider
{
	public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
	{
		using var scope = await Scope.Create().WithSystemIdentity();

		var settings = scope.ServiceProvider.GetRequiredService<ISettingService>();
		var value = await settings.Select(Dto.Factory.CreateName("DefaultCulture"));

		if (value is not null && !string.IsNullOrWhiteSpace(value.Value))
			return new ProviderCultureResult(value.Value, value.Value);

		await scope.Commit();

		return await Unresolved;
	}
}