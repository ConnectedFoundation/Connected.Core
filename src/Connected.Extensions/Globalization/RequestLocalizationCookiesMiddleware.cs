using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Connected.Globalization;
internal sealed class RequestLocalizationCookiesMiddleware : Microsoft.AspNetCore.Http.IMiddleware
{
	private CookieRequestCultureProvider? Provider { get; }
	private ILogger<RequestLocalizationCookiesMiddleware> Logger { get; }

	public RequestLocalizationCookiesMiddleware(IOptions<RequestLocalizationOptions> options, ILogger<RequestLocalizationCookiesMiddleware> logger)
	{
		var target = options.Value.RequestCultureProviders.Where(x => x is CookieRequestCultureProvider).Cast<CookieRequestCultureProvider>().First();

		if (target is not null)
			Provider = target;

		Logger = logger;
	}

	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		if (Provider is not null)
			SetCookie(context);

		if (next is not null && !context.Response.HasStarted)
			await next(context);
	}

	private void SetCookie(HttpContext context)
	{
		if (context.Features.Get<IRequestCultureFeature>() is IRequestCultureFeature feature)
		{
			if (context.Response.HasStarted)
			{
				Logger.LogError($"Unable to set culture provider cookie, request already started. ({context.Request.Path})");

				return;
			}

			if (Provider is not null)
				context.Response.Cookies.Append(Provider.CookieName, CookieRequestCultureProvider.MakeCookieValue(feature.RequestCulture));
		}
	}
}
