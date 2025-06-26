using Connected.Services;

namespace Connected.Identities.Globalization.Dtos;

public interface IInsertIdentityGlobalizationDto : IDto
{
	string Id { get; set; }
	string? TimeZone { get; set; }
	int? Language { get; set; }
}
