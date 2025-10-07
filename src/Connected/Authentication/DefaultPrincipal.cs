using System.Security.Claims;
using System.Security.Principal;

namespace Connected.Authentication;

public class DefaultPrincipal(IIdentity? identity) : ClaimsPrincipal
{
	public override IIdentity? Identity { get; } = identity;
}