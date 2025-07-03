using Connected.Runtime;

namespace Connected.Membership;

public class MembershipImage : RuntimeImage
{
	protected override void OnRegister()
	{
		Application.RegisterMicroService("Connected.Core.Membership.Authorization.dll");
		Application.RegisterMicroService("Connected.Core.Membership.Claims.dll");
		Application.RegisterMicroService("Connected.Core.Membership.Roles.dll");
	}
}
