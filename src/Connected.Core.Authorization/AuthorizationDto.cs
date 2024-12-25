using Connected.Services;

namespace Connected.Authorization;

internal class AuthorizationDto : Dto
{
	public IAuthorizationSchema? Schema { get; init; }
	public int? User { get; init; }
	public object? PrimaryKey { get; init; }
	public string? Claim { get; init; }
	public string? Entity { get; init; }
	public string? Component { get; init; }
	public string? Method { get; init; }
}
