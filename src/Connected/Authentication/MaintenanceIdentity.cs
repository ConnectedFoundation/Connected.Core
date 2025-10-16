using Connected.Identities;

namespace Connected.Authentication;
internal sealed class MaintenanceIdentity
	: IIdentity
{
	public required string Token { get; init; }
}
