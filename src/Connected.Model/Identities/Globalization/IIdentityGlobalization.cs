using Connected.Entities;

namespace Connected.Identities.Globalization;

public interface IIdentityGlobalization : IEntity<string>
{
	string? TimeZone { get; init; }
	int? Language { get; init; }
}