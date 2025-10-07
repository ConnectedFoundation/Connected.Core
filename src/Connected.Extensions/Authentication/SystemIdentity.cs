using Connected.Identities;

namespace Connected.Authentication;
internal sealed class SystemIdentity : IIdentity
{
	public SystemIdentity()
	{
		Token = "9A328F3D-C599-48FF-BE35-629C7673EE82";
	}

	public string Token { get; init; }
}
