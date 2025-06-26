using Connected.Services;

namespace Connected.Identities.MetaData.Dtos;

public interface IIdentityMetaDataDto : IPrimaryKeyDto<string>
{
	string? Url { get; set; }
	string? Description { get; set; }
	string? Avatar { get; set; }
	string? UserName { get; set; }
}
