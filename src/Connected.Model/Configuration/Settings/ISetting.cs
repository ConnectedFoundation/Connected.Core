using Connected.Entities;

namespace Connected.Configuration.Settings;

/// <summary>
/// Represents a configuration setting persisted as an entity.
/// </summary>
/// <remarks>
/// Exposes a strongly-typed integer primary key via <see cref="IEntity{T}"/> and two fields: a
/// unique setting name and an optional string value; consumers typically cache or query these
/// settings to drive runtime behavior.
/// </remarks>
public interface ISetting
	: IEntity<int>
{
	/// <summary>
	/// Gets the unique setting name.
	/// </summary>
	string Name { get; init; }
	/// <summary>
	/// Gets the stored value (if any) for the setting.
	/// </summary>
	string? Value { get; init; }
}
