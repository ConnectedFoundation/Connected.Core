using System.Collections.Immutable;

namespace Connected.Membership.Claims;

public abstract class ClaimProvider : Middleware, IClaimProvider
{
	public async Task<IImmutableList<string>> Invoke()
	{
		return await OnInvoke();
	}

	protected virtual Task<IImmutableList<string>> OnInvoke()
	{
		return Task.FromResult<IImmutableList<string>>(ImmutableList<string>.Empty);
	}
}