using Connected.Entities;

namespace Connected.Identities.Authentication;

public enum AuthenticationTokenStatus
{
	NotSet = 0,
	Enabled = 1,
	Disabled = 2
}

public interface IIdentityAuthenticationToken : IEntity<long>
{
	string Key { get; init; }
	string? Token { get; init; }
	string Identity { get; init; }
	AuthenticationTokenStatus Status { get; init; }
	DateTimeOffset? Expire { get; init; }
}