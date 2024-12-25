using Connected.Entities;

namespace Connected.Identities.MetaData;

public interface IIdentityMetaData : IEntity<string>
{
	string? Url { get; init; }
	string? Description { get; init; }
	string? Avatar { get; init; }
	string? UserName { get; init; }
}