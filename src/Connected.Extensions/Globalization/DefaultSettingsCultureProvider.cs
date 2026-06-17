using Connected.Authentication;
using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Globalization;

internal sealed class DefaultSettingsCultureProvider : CultureProviderBase, IRequestCultureProvider
{
	public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
	{
		using var scope = await Scope.Create().WithSystemIdentity();

		var settings = scope.ServiceProvider.GetRequiredService<IConfiguration>();
		var value = settings.GetSection("globalization:defaultCulture");

		try
		{
			if (value is not null && !string.IsNullOrWhiteSpace(value.Value))
				return new ProviderCultureResult(value.Value, value.Value);
		}
		finally
		{
			await scope.Commit();
		}

		return await Unresolved;
	}
}