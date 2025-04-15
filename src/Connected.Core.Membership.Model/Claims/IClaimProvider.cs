using System.Collections.Immutable;

namespace Connected.Membership.Claims;

public interface IClaimProvider : IMiddleware
{
	Task<IImmutableList<string>> Invoke();
}