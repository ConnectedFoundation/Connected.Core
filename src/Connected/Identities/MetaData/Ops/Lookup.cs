using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.MetaData.Ops;

internal sealed class Lookup(IIdentityMetaDataCache cache)
  : ServiceFunction<IPrimaryKeyListDto<string>, IImmutableList<IIdentityMetaData>>
{
	protected override async Task<IImmutableList<IIdentityMetaData>> OnInvoke()
	{
		return await cache.AsEntities(f => Dto.Items.Any(g => string.Equals(g, f.Id, StringComparison.OrdinalIgnoreCase)));
	}
}
