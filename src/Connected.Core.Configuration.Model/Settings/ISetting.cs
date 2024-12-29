using Connected.Entities;

namespace Connected.Configuration.Settings;

public interface ISetting : IEntity<int>
{
	string Name { get; init; }
	string? Value { get; init; }
}
