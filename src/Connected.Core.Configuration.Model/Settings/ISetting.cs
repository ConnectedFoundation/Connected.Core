using Connected.Entities;

namespace Connected.Configuration.Settings;

public interface ISetting : IPrimaryKeyEntity<int>
{
	string Name { get; init; }
	string? Value { get; init; }
}
