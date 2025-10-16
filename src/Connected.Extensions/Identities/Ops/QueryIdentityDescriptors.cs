using Connected.Identities.Schemas;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Ops;
internal sealed class QueryIdentityDescriptors(IMiddlewareService middlewares)
	: ServiceFunction<IValueListDto<string>, IImmutableList<IIdentityDescriptor>>
{
	protected override async Task<IImmutableList<IIdentityDescriptor>> OnInvoke()
	{
		var items = await middlewares.Query<IIdentityDescriptorProvider>();
		var unresolved = Dto;
		var result = new List<IIdentityDescriptor>();

		foreach (var middleware in items)
		{
			var resolved = await middleware.Query(unresolved);

			if (resolved.Count > 0)
			{
				result.AddRange(resolved);

				var dto = new Dto<IValueListDto<string>>().Value;

				dto.Items.AddRange(unresolved.Items.Except(resolved.Select(f => f.Token)));

				unresolved = dto;
			}
		}

		return result.ToImmutableList();
	}
}
