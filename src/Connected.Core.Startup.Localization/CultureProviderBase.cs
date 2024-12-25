﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace Connected.Startup.Localization;
internal abstract class CultureProviderBase : IRequestCultureProvider
{
	public static Task<ProviderCultureResult?> Unresolved => Task.FromResult<ProviderCultureResult?>(null);

	public abstract Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext);
}