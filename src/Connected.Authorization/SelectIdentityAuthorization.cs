using Connected.Annotations;
using Connected.Authentication;
using Connected.Authorization.Services;
using Connected.Services;

namespace Connected.Authorization;

[Middleware<IAuthenticationService>(nameof(IAuthenticationService.SelectIdentity))]
internal class SelectIdentityAuthorization : ServiceOperationAuthorization<IDto>
{
}
