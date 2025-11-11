using Connected.Entities;

namespace Connected.Identities;

/// <summary>
/// Defines the possible status values for a user account.
/// </summary>
/// <remarks>
/// This enumeration represents the operational state of a user account, controlling
/// access and authentication capabilities. The status determines whether a user can
/// actively use the system or is restricted.
/// </remarks>
public enum UserStatus
{
	/// <summary>
	/// The user account is disabled and cannot be used for authentication.
	/// </summary>
	Disabled = 0,

	/// <summary>
	/// The user account is active and can be used for authentication.
	/// </summary>
	Active = 1,

	/// <summary>
	/// The user account is locked and cannot be used for authentication until unlocked.
	/// </summary>
	Locked = 2
}

/// <summary>
/// Represents a user entity in the identity system.
/// </summary>
/// <remarks>
/// This interface defines the contract for user entities, combining entity and identity
/// characteristics with personal information such as name and email. The interface uses
/// init-only properties to ensure immutability after construction. Users are identified
/// by a long-typed primary key and include an operational status to control access.
/// </remarks>
public interface IUser
	: IEntity<long>, IIdentity
{
	/// <summary>
	/// Gets the user's first name.
	/// </summary>
	string? FirstName { get; init; }

	/// <summary>
	/// Gets the user's last name.
	/// </summary>
	string? LastName { get; init; }

	/// <summary>
	/// Gets the user's email address.
	/// </summary>
	string? Email { get; init; }

	/// <summary>
	/// Gets the operational status of the user account.
	/// </summary>
	UserStatus Status { get; init; }
}