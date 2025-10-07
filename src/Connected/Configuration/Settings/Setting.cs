using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Configuration.Settings;

[Table(Schema = SchemaAttribute.CoreSchema)]
internal sealed record Setting : ConcurrentEntity<int>, ISetting
{
	public const string EntityKey = $"{SchemaAttribute.CommonSchema}.{nameof(Setting)}";

	[Ordinal(1), Index(true), Length(128)]
	public string Name { get; init; } = default!;

	[Ordinal(2), Length(1024)]
	public string? Value { get; init; }
}