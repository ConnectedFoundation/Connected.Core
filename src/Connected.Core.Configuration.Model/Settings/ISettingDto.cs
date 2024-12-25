using Connected.Services;

namespace Connected.Configuration.Settings;

public interface ISettingDto : IDto
{
	string? Name { get; set; }
	string? Value { get; set; }
}
