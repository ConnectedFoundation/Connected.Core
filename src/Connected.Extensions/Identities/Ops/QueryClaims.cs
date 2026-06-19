using Connected.Authentication;
using Connected.Caching;
using Connected.Identities.Authentication;
using Connected.Identities.Dtos;
using Connected.Membership;
using Connected.Membership.Claims;
using Connected.Membership.Claims.Dtos;
using Connected.Membership.Roles;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Ops;

internal sealed class QueryClaims(
	IClaimService claims,
	IAuthenticationService authentication,
	IMembershipService membership,
	IRoleService roles)
	 : ServiceFunction<IDto, IImmutableList<IClaim>>
{
	protected override async Task<IImmutableList<IClaim>> OnInvoke()
	{
		var identity = await authentication.SelectIdentity();

		var identities = await MembershipUtils.ResolveIdentityTokens(identity!.Token, membership, roles);

		return await claims.Query(DtoFactory.Create<IQueryClaimDto>(f =>
		{
			f.Identities = identities.ToList();
		}));
	}
}