using Connected.Identities.Schemas;
using Connected.Services;

namespace Connected.Identities.Ops;
internal sealed class SelectIdentityDescriptor(IMiddlewareService middlewares)
	: ServiceFunction<IValueDto<string>, IIdentityDescriptor?>
{
	protected override async Task<IIdentityDescriptor?> OnInvoke()
	{
		var items = await middlewares.Query<IIdentityDescriptorProvider>();

		foreach (var middleware in items)
		{
			var identity = await middleware.Select(Dto);

			if (identity is not null)
				return identity;
		}

		return null;
	}
}
