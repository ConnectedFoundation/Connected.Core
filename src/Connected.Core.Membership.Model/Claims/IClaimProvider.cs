namespace Connected.Membership.Claims;

public interface IClaimProvider : IMiddleware
{
   Task<ImmutableList<string>> Invoke();
}