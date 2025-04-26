using Connected.Annotations;
using Connected.Authentication;
using Connected.Authorization.Services;

namespace Connected.Core.Authorization.Default;

[Middleware<IAuthenticationService>(nameof(IAuthenticationService.UpdateIdentity))]
internal class UpdateIdentityAuthorization : ServiceOperationAuthorization<IUpdateIdentityDto>
{
}
