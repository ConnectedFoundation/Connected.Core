using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Configuration.Settings;

internal sealed class Query(ISettingCache cache) : ServiceFunction<IQueryDto, IImmutableList<ISetting>>
{
	protected override Task<IImmutableList<ISetting>> OnInvoke()
	{
		return cache.AsEntities<ISetting>();
	}
}
