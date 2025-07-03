using Connected.Membership.Claims.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Claims.Ops;

internal sealed class QuerySchema(IMiddlewareService middlewares)
  : ServiceFunction<IQueryClaimSchemaDto, IImmutableList<IClaimSchema>>
{
	protected override async Task<IImmutableList<IClaimSchema>> OnInvoke()
	{
		var items = await middlewares.Query<IClaimSchemaProvider>();
		var result = new List<IClaimSchema>();

		foreach (var item in items)
		{
			var schema = await item.Invoke(Dto);

			foreach (var schemaItem in schema)
			{
				if (result.FirstOrDefault(f => string.Equals(f.Id, schemaItem.Id, StringComparison.OrdinalIgnoreCase)) is null)
					result.Add(schemaItem);
			}
		}

		return result.ToImmutableList();
	}
}
