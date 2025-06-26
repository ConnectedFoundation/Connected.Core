using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities.MetaData.Dtos;

internal abstract class IdentityMetaDataDto : Dto, IIdentityMetaDataDto
{
	[Required, MaxLength(256)]
	public required string Id { get; set; }

	[MaxLength(1024)]
	public string? Url { get; set; }

	[MaxLength(1024)]
	public string? Description { get; set; }

	[MaxLength(1024)]
	public string? Avatar { get; set; }

	[MaxLength(128)]
	public string? UserName { get; set; }
}
