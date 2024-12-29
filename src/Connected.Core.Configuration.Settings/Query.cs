using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Configuration.Settings;

internal sealed class Query(ISettingCache cache) : ServiceFunction<IQueryDto, ImmutableList<ISetting>>
{
	protected override Task<ImmutableList<ISetting>> OnInvoke()
	{
		return cache.AsEntities<ISetting>();
	}
}
