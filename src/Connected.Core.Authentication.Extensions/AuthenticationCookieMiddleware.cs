using Connected.Configuration;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Connected.Authentication;
internal class AuthenticationCookieMiddleware
{
	private readonly RequestDelegate _next;
	private bool _initialized;

	public AuthenticationCookieMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	private string? AuthenticationCookieName { get; set; }

	public async Task InvokeAsync(HttpContext context)
	{
		await Initialize();
		await SlideCookie(context);

		if (_next is not null && !context.Response.HasStarted)
			await _next(context);
	}

	private async Task Initialize()
	{
		if (_initialized)
			return;

		_initialized = true;

		AuthenticationCookieName = await SettingUtils.GetValue<string>("Authentication Cookie Name") ?? "ConnectedAuthenticationCookie";
	}

	private async Task SlideCookie(HttpContext context)
	{
		if (AuthenticationCookieName is null)
			return;

		var cookie = context.Request.Cookies[AuthenticationCookieName];

		if (string.IsNullOrWhiteSpace(cookie))
			return;

		using var ms = new MemoryStream(Convert.FromBase64String(cookie));
		var json = JsonNode.Parse(ms);
		var expiration = 0L;

		if (json is null)
			return;

		if (json["expiration"] is JsonNode expirationNode)
			expiration = expirationNode.GetValue<long>();

		if (expiration == 0)
			return;

		var dt = new DateTimeOffset(expiration, TimeSpan.Zero);

		if (dt < DateTimeOffset.UtcNow)
			return;

		var duration = TimeSpan.FromMinutes(20);

		if (json["renewDuration"] is JsonNode renewDurationNode)
			duration = TimeSpan.FromSeconds(renewDurationNode.GetValue<int>());

		if (dt < DateTimeOffset.UtcNow.AddSeconds(duration.TotalSeconds / 2))
		{
			var expires = DateTimeOffset.UtcNow.Add(duration);
			var node = await JsonNode.ParseAsync(ms);

			if (node is null)
				return;

			node["expiration"] = expires.Ticks;

			using var ws = new MemoryStream();
			using var writer = new Utf8JsonWriter(ws);

			node.WriteTo(writer);

			writer.Flush();
			ws.Seek(0, SeekOrigin.Begin);

			var cookieContent = Convert.ToBase64String(ms.ToArray());

			context.Response.Cookies.Delete(AuthenticationCookieName);
			context.Response.Cookies.Append(AuthenticationCookieName, cookieContent, new CookieOptions
			{
				HttpOnly = true,
				Expires = expires
			});
		}
	}
}
