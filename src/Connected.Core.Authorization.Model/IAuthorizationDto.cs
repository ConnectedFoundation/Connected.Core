using Connected.Services;

namespace Connected.Authorization;
public interface IAuthorizationDto : IDto
{
	IAuthorizationSchema? Schema { get; init; }
	string? Identity { get; init; }
	object? PrimaryKey { get; init; }
	string? Claim { get; init; }
	string? Entity { get; init; }
	string? Component { get; init; }
	string? Method { get; init; }
}
