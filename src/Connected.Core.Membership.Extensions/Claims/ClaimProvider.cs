﻿using System.Collections.Immutable;

namespace Connected.Membership.Claims;

public abstract class ClaimProvider : Middleware, IClaimProvider
{
	public async Task<ImmutableList<string>> Invoke()
	{
		return await OnInvoke();
	}

	protected virtual Task<ImmutableList<string>> OnInvoke()
	{
		return Task.FromResult(ImmutableList<string>.Empty);
	}
}