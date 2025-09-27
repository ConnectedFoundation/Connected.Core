using Connected.Authentication;
using Connected.Authorization;
using Connected.Membership.Annotations;
using System.Reflection;

namespace Connected.Membership.Claims;

internal sealed class ClaimDecorationHandler(IClaimService claims, IAuthenticationService authentication) : AuthorizationDecorationHandler
{
	protected override async Task<AuthorizationResult> OnInvoke()
	{
		var attributes = Dto.Middleware.GetType().GetCustomAttributes<ClaimsAttribute>(true);
		var onePassed = false;

		foreach (var attribute in attributes)
		{
			var result = await ProcessAttribute(Dto.Middleware, attribute);

			if (result == AuthorizationResult.Fail)
				return AuthorizationResult.Fail;
			else if (result == AuthorizationResult.Pass)
				onePassed = true;
		}

		if (attributes.Count() == 0)
			return AuthorizationResult.Skip;

		if (onePassed)
			return AuthorizationResult.Pass;

		return AuthorizationResult.Skip;
	}

	private async Task<AuthorizationResult> ProcessAttribute(IAuthorization authorization, ClaimsAttribute attribute)
	{
		var tokens = attribute.Claims.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		var identity = await authentication.SelectIdentity();
		var onePassed = false;

		foreach (var token in tokens)
		{
			var hasClaim = await identity.HasClaim(claims, token, authorization.Entity, authorization.EntityId);

			if (!hasClaim && attribute.Options == ClaimsOptions.All)
				return AuthorizationResult.Fail;

			if (hasClaim)
				onePassed = true;
		}

		if (!onePassed)
			return AuthorizationResult.Fail;

		return AuthorizationResult.Pass;
	}
}
