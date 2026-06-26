using Connected.Annotations.Entities;
using Connected.Configuration.Settings;

namespace Connected.Configuration;

public static class ConfigurationMetaData
{
	public const string SettingKey = $"{SchemaAttribute.CoreSchema}.{nameof(ISetting)}";
}