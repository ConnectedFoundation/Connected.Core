using Connected.Identities.Authentication;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Identities;
public static class IdentityAuthenticationTokenExtensions
{
	public static bool IsValid(this IIdentityAuthenticationToken value)
	{
		return value.Status == AuthenticationTokenStatus.Enabled && (value.Expire is null || value.Expire < DateTimeOffset.UtcNow);
	}

	public static async Task<string?> AuthenticationToken(this IIdentity identity)
	{
		using var scope = Scope.Create();

		var tokenService = scope.ServiceProvider.GetRequiredService<IIdentityAuthenticationTokenService>();
		var dto = scope.ServiceProvider.GetRequiredService<IQueryIdentityAuthenticationTokensDto>();

		dto.Identity = identity.Token;
		dto.Key = IdentityAuthenticationTokens.AuthenticationToken;

		try
		{
			var tokens = await tokenService.Query(dto);

			if (tokens.IsEmpty)
				return null;

			var target = tokens.FirstOrDefault(f => f.IsValid());

			if (target is null)
				return null;

			return target.Token;
		}
		catch
		{
			await scope.Rollback();

			return null;
		}
		finally
		{
			await scope.Commit();
		}
	}
}
