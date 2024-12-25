using Connected.Services;

namespace Connected.Identities.MetaData;

public interface IInsertIdentityMetaDataDto : IDto
{
	string Id { get; set; }
	string? Url { get; set; }
	string? Description { get; set; }
	string? Avatar { get; set; }
	string? UserName { get; set; }
}
