using Connected.Annotations;
using Connected.Authentication;
using Connected.Authorization.Services;
using Connected.Services;

namespace Connected.Core.Authorization.Default;

[Middleware<IAuthenticationService>(nameof(IAuthenticationService.SelectIdentity))]
internal class SelectIdentityAuthorization : ServiceOperationAuthorization<IDto>
{
	public override string Entity => NullAuthorizationEntity;
	public override string EntityId => NullAuthorizationEntityId;
}
