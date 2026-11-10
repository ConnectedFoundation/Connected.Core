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

	/// <summary>
	/// Determines whether a token is enabled, has not expired and has not exceeded the configured maximum lifetime.
	/// </summary>
	/// <param name="value">The token to evaluate.</param>
	/// <param name="configuration">The token lifetime configuration.</param>
	/// <returns><see langword="true"/> when the token may still be used.</returns>
	public static bool IsValid(this IIdentityAuthenticationToken value, IAuthenticationTokenConfiguration configuration)
	{
		return value.IsValid() && !configuration.IsBeyondLifetime(value);
	}

	/// <summary>
	/// Determines whether an expiring token has reached the end of the configured maximum lifetime.
	/// </summary>
	/// <param name="configuration">The token lifetime configuration.</param>
	/// <param name="token">The token to evaluate.</param>
	/// <returns><see langword="true"/> when a maximum lifetime is configured and the token has reached it.</returns>
	/// <remarks>
	/// Permanent tokens and tokens without a creation time are never beyond their lifetime.
	/// </remarks>
	public static bool IsBeyondLifetime(this IAuthenticationTokenConfiguration configuration, IIdentityAuthenticationToken token)
	{
		if (token.Expire is null || token.Created is null || configuration.LifetimeLimit(token.Created.Value) is not DateTimeOffset limit)
			return false;

		return limit <= DateTimeOffset.UtcNow;
	}

	public static async Task<string?> AuthenticationToken(this IIdentity identity)
	{
		return await AuthenticationToken(identity, null, false);
	}

	/// <summary>
	/// Returns the identity's authentication token when it can be handed out at sign-in.
	/// </summary>
	/// <param name="identity">The identity the authentication token belongs to.</param>
	/// <param name="configuration">The token lifetime configuration.</param>
	/// <returns>The value of a valid token that is not spent, or <see langword="null"/> when a new token must be issued.</returns>
	public static async Task<string?> SignInToken(this IIdentity identity, IAuthenticationTokenConfiguration configuration)
	{
		return await AuthenticationToken(identity, configuration, true);
	}

	private static async Task<string?> AuthenticationToken(IIdentity identity, IAuthenticationTokenConfiguration? tokenConfiguration, bool excludeSpent)
	{
		await using var scope = await Scope.Create().WithSystemIdentity();

		var tokenService = scope.ServiceProvider.GetRequiredService<IIdentityAuthenticationTokenService>();
		var configuration = tokenConfiguration ?? scope.ServiceProvider.GetService<IConfigurationService>()?.Authentication.Tokens ?? IAuthenticationTokenConfiguration.Unbounded;
		var dto = scope.ServiceProvider.GetRequiredService<IQueryIdentityAuthenticationTokensDto>();

		dto.Identity = identity.Token;
		dto.Key = IdentityAuthenticationTokens.AuthenticationToken;

		try
		{
			var tokens = await tokenService.Query(dto);

			if (tokens.Count == 0)
				return null;

			var target = tokens.FirstOrDefault(f => f.IsValid(configuration) && !(excludeSpent && configuration.IsSpent(f)));

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
		var expire = DateTimeOffset.UtcNow.AddMinutes(configuration.ResolveIdleTimeout(defaultTimeout));

		/*
		 * Tokens without a creation time were issued before it was recorded and cannot be bounded.
		 */
		if (created is null || configuration.LifetimeLimit(created.Value) is not DateTimeOffset limit)
			return expire;

		return limit < expire ? limit : expire;
	}

	/// <summary>
	/// Resolves the number of minutes an expiring token stays valid without activity.
	/// </summary>
	/// <param name="configuration">The token lifetime configuration.</param>
	/// <param name="defaultTimeout">The number of minutes to use when no positive idle timeout is configured.</param>
	/// <returns>The configured idle timeout, or <paramref name="defaultTimeout"/>.</returns>
	private static int ResolveIdleTimeout(this IAuthenticationTokenConfiguration configuration, int defaultTimeout)
	{
		return configuration.IdleTimeout is int configured && configured > 0 ? configured : defaultTimeout;
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
		var configuration = ServiceExtensionsStartup.Services?.GetService<IConfigurationService>()?.Authentication.Tokens ?? IAuthenticationTokenConfiguration.Unbounded;

		return await service.Ensure(configuration, identity, proposedToken, permanent);
	}

	/// <summary>
	/// Determines whether signing in should issue a new token instead of reusing the given one.
	/// </summary>
	/// <param name="configuration">The token lifetime configuration.</param>
	/// <param name="token">The existing token of the identity.</param>
	/// <returns>
	/// <see langword="true"/> when the token is an expiring token that has expired, or that can no longer be given a
	/// full idle period before the end of its maximum lifetime.
	/// </returns>
	/// <remarks>
	/// An expired token is never revived, so a bearer value that has lapsed cannot become valid again. Permanent
	/// tokens are never spent. When the maximum lifetime is not longer than the idle timeout, no token could ever be
	/// given a full idle period, so only expired tokens are spent; otherwise every sign-in would replace the token.
	/// </remarks>
	public static bool IsSpent(this IAuthenticationTokenConfiguration configuration, IIdentityAuthenticationToken token)
	{
		if (token.Expire is null)
			return false;

		var now = DateTimeOffset.UtcNow;

		if (token.Expire <= now)
			return true;

		if (token.Created is null || configuration.MaxLifetime is not int maxLifetime || maxLifetime <= 0)
			return false;

		var idleTimeout = configuration.ResolveIdleTimeout(DefaultSignInTimeout);

		if (maxLifetime <= idleTimeout)
			return false;

		return token.Created.Value.AddMinutes(maxLifetime) < now.AddMinutes(idleTimeout);
	}

	/// <summary>
	/// Determines whether signing in must replace the token instead of updating it in place.
	/// </summary>
	/// <param name="configuration">The token lifetime configuration.</param>
	/// <param name="token">The existing token of the identity.</param>
	/// <param name="permanent">Whether the sign-in asks for a permanent token.</param>
	/// <returns>
	/// <see langword="true"/> when the token is spent, or when a permanent token becomes expiring under a maximum
	/// lifetime: its creation time marks the start of the remembered session, not of this one, so the maximum
	/// lifetime could already have passed.
	/// </returns>
	private static bool MustReplace(IAuthenticationTokenConfiguration configuration, IIdentityAuthenticationToken token, bool permanent)
	{
		if (configuration.IsSpent(token))
			return true;

		return !permanent && token.Expire is null && token.Created is DateTimeOffset created && configuration.LifetimeLimit(created) is not null;
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
		 * A token that has lapsed or is about to reach the end of its lifetime is not revived. Issuing a new
		 * token starts a new lifetime. The token is shared by every session of the identity, so replacing it
		 * near the end of its lifetime ends the other sessions early; they could not have outlived the limit.
		 */
		if (existingKey is not null && existingKey.Status == AuthenticationTokenStatus.Enabled && MustReplace(configuration, existingKey, permanent))
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
