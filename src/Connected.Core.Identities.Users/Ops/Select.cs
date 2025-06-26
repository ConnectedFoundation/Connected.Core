﻿using Connected.Services;

namespace Connected.Identities.Ops;

internal sealed class Select(IUserCache cache)
  : ServiceFunction<IPrimaryKeyDto<long>, IUser?>
{
	protected override async Task<IUser?> OnInvoke()
	{
		return await cache.Get(Dto.Id);
	}
}
