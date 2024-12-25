using Connected.Entities;

namespace Connected.Identities;

public enum UserStatus
{
	Disabled = 0,
	Active = 1,
	Locked = 2
}

public interface IUser : IEntity<long>, IIdentity
{
	string? FirstName { get; init; }
	string? LastName { get; init; }
	string? Email { get; init; }

	UserStatus Status { get; init; }

	DateTimeOffset Created { get; init; }

	string? Password { get; init; }
}