using Connected.Services;

namespace Connected.Configuration.Settings;

public interface IUpdateSettingDto : IDto
{
	string Name { get; set; }
	string? Value { get; set; }
}
