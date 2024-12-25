using System.Collections.Immutable;

namespace Connected.Membership.Claims;

public interface IClaimProvider : IMiddleware
{
	Task<ImmutableList<string>> Invoke();
}