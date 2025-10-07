
using Connected.Identities;

namespace Connected.Authentication;

internal sealed class HttpIdentity(IIdentity? identity)
	: System.Security.Principal.IIdentity, IIdentityAccessor
{
	public string? AuthenticationType { get; } = "Connected";
	public bool IsAuthenticated { get; init; }
	public string? Name { get; init; }

	public IIdentity? Identity { get; } = identity;
}
