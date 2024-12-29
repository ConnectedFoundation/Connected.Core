using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Configuration.Settings;
internal sealed class UpdateSettingDto : Dto, IUpdateSettingDto
{
	[Required, MaxLength(128)]
	public string Name { get; set; } = default!;

	[MaxLength(1024)]
	public string? Value { get; set; }
}
