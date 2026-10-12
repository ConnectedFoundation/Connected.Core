using Connected.Annotations;
using Connected.Identities.Authentication;
using Connected.Identities.Ops;
using Connected.Membership.Claims;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities;

[Service, ServiceUrl(IdentitiesUrls.Identities)]
public interface IIdentityExtensions
{
	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl("select-descriptor")]
	Task<IIdentityDescriptor?> Select(IValueDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Get | ServiceOperationVerbs.Post), ServiceUrl("query-descriptors")]
	Task<IImmutableList<IIdentityDescriptor>> Query(IValueListDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task<string?> Reset(IValueDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task Ping(IValueDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl("query-claims")]
	Task<IImmutableList<IClaim>> QueryClaims();

	/// <summary>
	/// Returns the authentication token record, so a client can read when its session expires
	/// without guessing from <see cref="Ping"/>, which returns nothing.
	/// </summary>
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IIdentityAuthenticationToken?> TokenInfo(IValueDto<string> dto);

	/// <summary>
	/// Ends the identity's session server-side by disabling its authentication token, rather than
	/// only clearing it in the caller's own browser.
	/// </summary>
	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task SignOut(IValueDto<string> dto);
}
