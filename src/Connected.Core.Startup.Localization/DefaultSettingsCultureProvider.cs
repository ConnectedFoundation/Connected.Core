using Connected.Configuration.Settings;
using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Startup.Localization;
internal sealed class DefaultSettingsCultureProvider : CultureProviderBase, IRequestCultureProvider
{
	public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
	{
		using var scope = Bootstrapper.Services.CreateAsyncScope();

		var settings = scope.ServiceProvider.GetRequiredService<ISettingService>();
		var value = await settings.Select((NameDto)"DefaultCulture");

		if (value is not null && !string.IsNullOrWhiteSpace(value.Value))
			return new ProviderCultureResult(value.Value, value.Value);

		await scope.Commit();

		return await Unresolved;
	}
}