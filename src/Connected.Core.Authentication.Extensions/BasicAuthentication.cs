using Connected.Identities;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Authentication.Basic;

internal sealed class BasicAuthentication : BasicAuthenticationProvider
{
	protected override async Task OnInvoke()
	{
		if (UserName is null || Password is null)
			return;

		using var scope = Scope.Create();

		try
		{
			var authentication = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
			var users = scope.ServiceProvider.GetRequiredService<IUserService>();
			var dto = scope.ServiceProvider.GetRequiredService<ISelectUserDto>();

			dto.User = UserName;
			dto.Password = Password;

			var user = await users.Select(dto);

			if (user is null)
				return;

			var identityDto = scope.ServiceProvider.GetRequiredService<IUpdateIdentityDto>();

			identityDto.Identity = user;

			await authentication.UpdateIdentity(identityDto);
		}
		catch
		{
			await scope.Rollback();

			throw;
		}
		finally
		{
			await scope.Flush();
		}
	}
}