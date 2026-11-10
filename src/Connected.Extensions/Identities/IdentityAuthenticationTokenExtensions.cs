using Connected.Authentication;
using Connected.Configuration;
using Connected.Configuration.Authentication;
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
		await using var scope = await Scope.Create().WithSystemIdentity();

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

			if (target.Expire is null)
				return target.Token;

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

	/// <summary>
	/// The number of minutes an expiring token stays valid after sign-in when no idle timeout is configured.
	/// </summary>
	internal const int DefaultSignInTimeout = 30;
	/// <summary>
	/// The number of minutes a ping or reset extends an expiring token when no idle timeout is configured.
	/// </summary>
	internal const int DefaultRenewTimeout = 20;

	/// <summary>
	/// Resolves the expiration time of an expiring token that is issued or extended now.
	/// </summary>
	/// <param name="configuration">The token lifetime configuration.</param>
	/// <param name="created">The creation time of the token, or <see langword="null"/> when it is unknown.</param>
	/// <param name="defaultTimeout">The number of minutes to use when no idle timeout is configured.</param>
	/// <returns>The current time plus the idle timeout, bounded by the token's maximum lifetime when one is configured.</returns>
	/// <remarks>
	/// Values that are not positive are treated as not configured, so a misconfiguration cannot make every token
	/// lapse as soon as it is issued.
	/// </remarks>
	public static DateTimeOffset ResolveExpire(this IAuthenticationTokenConfiguration configuration, DateTimeOffset? created, int defaultTimeout)
	{
		var idleTimeout = configuration.IdleTimeout is int configured && configured > 0 ? configured : defaultTimeout;
		var expire = DateTimeOffset.UtcNow.AddMinutes(idleTimeout);

		/*
		 * Tokens without a creation time were issued before it was recorded and cannot be bounded.
		 */
		if (created is null || configuration.LifetimeLimit(created.Value) is not DateTimeOffset limit)
			return expire;

		return limit < expire ? limit : expire;
	}

	/// <summary>
	/// Resolves the time after which a token created at the given time may no longer be extended.
	/// </summary>
	/// <param name="configuration">The token lifetime configuration.</param>
	/// <param name="created">The creation time of the token.</param>
	/// <returns>The end of the token's maximum lifetime, or <see langword="null"/> when no positive maximum lifetime is configured.</returns>
	public static DateTimeOffset? LifetimeLimit(this IAuthenticationTokenConfiguration configuration, DateTimeOffset created)
	{
		return configuration.MaxLifetime is int maxLifetime && maxLifetime > 0 ? created.AddMinutes(maxLifetime) : null;
	}

	public static async Task<string> Ensure(this IIdentityAuthenticationTokenService service, string identity, string proposedToken, bool permanent)
	{
		/*
		 * Callers of this overload do not pass a configuration, so the application's configuration is used when
		 * it is available. Otherwise a configured maximum lifetime would be ignored by such callers.
		 */
		var configuration = ServiceExtensionsStartup.Services?.GetService<IConfigurationService>()?.Authentication.Tokens ?? DefaultTokenConfiguration.Instance;

		return await service.Ensure(configuration, identity, proposedToken, permanent);
	}

	/// <summary>
	/// Determines whether a token can no longer be extended under a configured maximum lifetime.
	/// </summary>
	/// <param name="configuration">The token lifetime configuration.</param>
	/// <param name="token">The token to evaluate.</param>
	/// <returns><see langword="true"/> when a maximum lifetime is configured and the token has expired or reached it.</returns>
	private static bool IsSpent(IAuthenticationTokenConfiguration configuration, IIdentityAuthenticationToken token)
	{
		if (token.Created is null || configuration.LifetimeLimit(token.Created.Value) is not DateTimeOffset limit)
			return false;

		var now = DateTimeOffset.UtcNow;

		return limit <= now || token.Expire is not null && token.Expire <= now;
	}

	/// <summary>
	/// Returns the identity's authentication token, issuing or refreshing it with the configured lifetime.
	/// </summary>
	/// <param name="service">The authentication token service.</param>
	/// <param name="configuration">The token lifetime configuration.</param>
	/// <param name="identity">The token of the identity the authentication token belongs to.</param>
	/// <param name="proposedToken">The token value to use when a new authentication token is issued.</param>
	/// <param name="permanent">Whether the authentication token never expires.</param>
	/// <returns>The value of the identity's authentication token.</returns>
	public static async Task<string> Ensure(this IIdentityAuthenticationTokenService service, IAuthenticationTokenConfiguration configuration, string identity, string proposedToken, bool permanent)
	{
		var dto = new Dto<IQueryIdentityAuthenticationTokensDto>().Value;

		dto.Identity = identity;
		dto.Key = IdentityAuthenticationTokens.AuthenticationToken;

		var existingKey = (await service.Query(dto)).FirstOrDefault(f => string.Equals(f.Key, IdentityAuthenticationTokens.AuthenticationToken, StringComparison.Ordinal));

		/*
		 * With a maximum lifetime, a token that has lapsed or reached the end of its lifetime must not be
		 * revived, since its creation time would otherwise end the new sign-in immediately. Issuing a new
		 * token starts a new lifetime.
		 */
		if (existingKey is not null && existingKey.Status == AuthenticationTokenStatus.Enabled && IsSpent(configuration, existingKey))
		{
			await service.Delete(DtoFactory.Create<IPrimaryKeyDto<long>>(f => f.Id = existingKey.Id));

			existingKey = null;
		}

		if (existingKey is not null && existingKey.Status == AuthenticationTokenStatus.Enabled && existingKey.Token is not null)
		{
			var tokensDto = existingKey.AsDto<IUpdateIdentityAuthenticationTokenDto>();
			DateTimeOffset? expire = permanent ? null : configuration.ResolveExpire(existingKey.Created, DefaultSignInTimeout);

			tokensDto.Expire = expire;
			tokensDto.Status = AuthenticationTokenStatus.Enabled;

			await service.Update(tokensDto);

			return existingKey.Token;
		}
		else
		{
			var tokensDto = new Dto<IInsertIdentityAuthenticationTokenDto>().Value;

			tokensDto.Identity = identity;
			tokensDto.Key = IdentityAuthenticationTokens.AuthenticationToken;
			tokensDto.Token = proposedToken;
			tokensDto.Status = AuthenticationTokenStatus.Enabled;

			if (permanent)
				tokensDto.Expire = null;
			else
				tokensDto.Expire = configuration.ResolveExpire(DateTimeOffset.UtcNow, DefaultSignInTimeout);

			await service.Insert(tokensDto);

			return proposedToken;
		}
	}
}
