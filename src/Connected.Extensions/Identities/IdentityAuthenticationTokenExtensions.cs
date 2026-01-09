using Connected.Authentication;
using Connected.Identities.Authentication;
using Connected.Identities.Authentication.Dtos;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Identities;

public static class IdentityAuthenticationTokenExtensions
{
	public static bool IsValid(this IIdentityAuthenticationToken value)
	{
		return value.Status == AuthenticationTokenStatus.Enabled && (value.Expire is null || value.Expire > DateTimeOffset.UtcNow);
	}

	public static async Task<string?> AuthenticationToken(this IIdentity identity)
	{
		using var scope = await Scope.Create().WithSystemIdentity();

		var tokenService = scope.ServiceProvider.GetRequiredService<IIdentityAuthenticationTokenService>();
		var dto = scope.ServiceProvider.GetRequiredService<IQueryIdentityAuthenticationTokensDto>();

		dto.Identity = identity.Token;
		dto.Key = IdentityAuthenticationTokens.AuthenticationToken;

		try
		{
			var tokens = await tokenService.Query(dto);

			if (tokens.Count == 0)
				return null;

			var target = tokens.FirstOrDefault(f => f.IsValid());

			if (target is null)
				return null;

			if (target.Expire is not null && target.Expire.Value >= DateTimeOffset.UtcNow)
				return target.Token;
			else
				return null;
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

	public static async Task Ensure(this IIdentityAuthenticationTokenService service, string identity, string token, bool permanent)
	{
		var dto = new Dto<IQueryIdentityAuthenticationTokensDto>().Value;

		dto.Identity = identity;
		dto.Key = IdentityAuthenticationTokens.AuthenticationToken;

		var existingKey = (await service.Query(dto)).FirstOrDefault(f => string.Equals(f.Key, IdentityAuthenticationTokens.AuthenticationToken, StringComparison.Ordinal));

		if (existingKey is not null)
		{
			var tokensDto = existingKey.AsDto<IUpdateIdentityAuthenticationTokenDto>();
			DateTimeOffset? expire = permanent ? null : DateTimeOffset.UtcNow.AddMinutes(30);

			tokensDto.Token = token;
			tokensDto.Expire = expire;

			await service.Update(tokensDto);
		}
		else
		{
			var tokensDto = new Dto<IInsertIdentityAuthenticationTokenDto>().Value;

			tokensDto.Identity = identity;
			tokensDto.Key = IdentityAuthenticationTokens.AuthenticationToken;
			tokensDto.Token = token;

			if (permanent)
				tokensDto.Expire = null;
			else
				tokensDto.Expire = DateTimeOffset.UtcNow.AddMinutes(30);

			await service.Insert(tokensDto);
		}
	}
}
