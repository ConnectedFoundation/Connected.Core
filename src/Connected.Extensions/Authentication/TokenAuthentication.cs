using Connected.Identities;
using Connected.Identities.Authentication;
using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Authentication;
internal sealed class TokenAuthentication(IHttpContextAccessor http)
	: BearerAuthenticationProvider
{
	protected override async Task OnAuthenticate()
	{
		if (Token is null)
			return;

		using var scope = await Scope.Create().WithSystemIdentity();

		var authentication = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
		var tokens = scope.ServiceProvider.GetRequiredService<IIdentityAuthenticationTokenService>();
		var token = await tokens.Select(Dto.CreateValue(Token));

		if (token is null)
			return;

		if (token.Expire < DateTimeOffset.UtcNow)
		{
			await tokens.Delete(Dto.CreatePrimaryKey(token.Id));

			return;
		}

		if (token.Status == AuthenticationTokenStatus.Disabled)
			return;

		var identityDto = new Dto<IUpdateIdentityDto>().Value;
		var extensions = scope.ServiceProvider.GetRequiredService<IIdentityExtensions>();
		var dto = new Dto<IValueDto<string>>().Value;

		dto.Value = token.Identity;

		identityDto.Identity = await extensions.Select(dto);

		if (identityDto.Identity is not null)
		{
			await authentication.UpdateIdentity(identityDto);


			if (http.HttpContext is not null)
			{
				http.HttpContext.User = new DefaultPrincipal(new HttpIdentity(identityDto.Identity)
				{
					IsAuthenticated = true
				});
			}

		}

		await scope.Flush();
	}
}
