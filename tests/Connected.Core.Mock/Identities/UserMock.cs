using Connected.Core.Entities.Mock;
using Connected.Identities;

namespace Connected.Core.Identities.Mock;
public class UserMock : EntityMock<long>, IUser
{
	public string? FirstName { get; init; }
	public string? LastName { get; init; }
	public string? Email { get; init; }
	public UserStatus Status { get; init; }
	public string? Password { get; init; }
	public required string Token { get; init; }
}
