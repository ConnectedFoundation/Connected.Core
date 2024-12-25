using Connected.Services;

namespace Connected.Identities.Globalization;

public interface IUpdateIdentityGlobalizationDto : IDto
{
	string Id { get; set; }
	string? TimeZone { get; set; }
	int? Language { get; set; }
}
