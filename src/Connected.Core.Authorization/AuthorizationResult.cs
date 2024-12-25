namespace Connected.Authorization;

public sealed class AuthorizationResult : IAuthorizationResult
{
	public bool Success { get; init; }

	public AuthorizationResultReason Reason { get; init; }

	public int PermissionCount { get; init; }

	public static AuthorizationResult OK() => new() { Success = true, Reason = AuthorizationResultReason.OK };
	public static AuthorizationResult Fail(AuthorizationResultReason reason) => new() { Success = false, Reason = reason };
}
