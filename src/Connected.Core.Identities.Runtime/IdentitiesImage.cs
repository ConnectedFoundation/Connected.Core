using Connected.Runtime;

namespace Connected.Identities;

public class IdentitiesImage : RuntimeImage
{
	protected override void OnRegister()
	{
		Application.RegisterMicroService("Connected.Core.Identities.Authentication.dll");
		Application.RegisterMicroService("Connected.Core.Identities.Globalization.dll");
		Application.RegisterMicroService("Connected.Core.Identities.MetaData.dll");
		Application.RegisterMicroService("Connected.Core.Identities.Users.dll");
	}
}
