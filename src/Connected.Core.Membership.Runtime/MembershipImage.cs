using Connected.Membership.Claims;
using Connected.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Membership;

public class MembershipImage : RuntimeImage
{
	protected override void OnRegister()
	{
		Application.DiscoverType += OnDiscoverType;

		Application.RegisterMicroService("Connected.Core.Membership.Authorization.dll");
		Application.RegisterMicroService("Connected.Core.Membership.Claims.dll");
		Application.RegisterMicroService("Connected.Core.Membership.Roles.dll");
		Application.RegisterMicroService("Connected.Core.Membership.dll");
	}

	private void OnDiscoverType(object sender, MicroServiceTypeDiscoveryEventArgs e)
	{
		AddClaimProvider(e.Type, e.Services);
	}

	private static void AddClaimProvider(Type type, IServiceCollection services)
	{
		var fullName = typeof(IClaimSchemaProvider).FullName;

		if (fullName is null)
			return;

		if (type.GetInterface(fullName) is null)
			return;

		services.Add(ServiceDescriptor.Scoped(type, type));
	}

}
