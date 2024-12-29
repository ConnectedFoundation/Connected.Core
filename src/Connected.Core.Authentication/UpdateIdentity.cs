using Connected.Services;
using Microsoft.AspNetCore.Http;

namespace Connected.Authentication;
internal sealed class UpdateIdentity(IAuthenticationService authentication, IHttpContextAccessor? http) : ServiceAction<IUpdateIdentityDto>
{
	protected override async Task OnInvoke()
	{
		if (authentication is AuthenticationService service)
			service.Identity = Dto.Identity;

		if (http?.HttpContext is not null)
		{
			var principal = new DefaultPrincipal(new HttpIdentity(Dto.Identity)
			{
				IsAuthenticated = Dto.Identity is not null,
				Name = Dto.Identity is null ? null : Dto.Identity.Token
			});

			http.HttpContext.User = principal;
		}

		await Task.CompletedTask;
	}
}
