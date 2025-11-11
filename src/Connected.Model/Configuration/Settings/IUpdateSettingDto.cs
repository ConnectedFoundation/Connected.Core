using Connected.Services;

namespace Connected.Configuration.Settings;
/// <summary>
/// DTO used to update a configuration setting's value.
/// </summary>
/// <remarks>
/// Carries the unique setting name and its new optional value for persistence by the settings service.
/// </remarks>
public interface IUpdateSettingDto
	: IDto
{
	/// <summary>
	/// Gets or sets the unique setting name.
	/// </summary>
	string Name { get; set; }
	/// <summary>
	/// Gets or sets the value to persist for the setting.
	/// </summary>
	string? Value { get; set; }
}
