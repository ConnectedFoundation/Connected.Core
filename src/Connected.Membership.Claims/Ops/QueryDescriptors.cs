using Connected.Membership.Claims.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Claims.Ops;

internal sealed class QueryDescriptors(IMiddlewareService middlewares)
  : ServiceFunction<IQueryClaimDescriptorsDto, IImmutableList<IClaimDescriptor>>
{
	protected override async Task<IImmutableList<IClaimDescriptor>> OnInvoke()
	{
		var items = await middlewares.Query<IClaimDescriptorProvider>();
		var result = new List<IClaimDescriptor>();

		foreach (var item in items)
		{
			var descriptors = await item.Invoke(Dto);

			foreach (var descriptor in descriptors)
			{
				if (result.FirstOrDefault(f => string.Equals(f.Id, descriptor.Id, StringComparison.OrdinalIgnoreCase)) is null)
					result.Add(descriptor);
			}
		}

		return result.ToImmutableList();
	}
}
