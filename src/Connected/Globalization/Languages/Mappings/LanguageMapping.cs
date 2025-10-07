using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Globalization.Languages.Mappings;

[Table(Schema = SchemaAttribute.CoreSchema)]
internal sealed record LanguageMapping : ConsistentEntity<int>, ILanguageMapping
{
	[Ordinal(0)]
	public int Language { get; init; }

	[Ordinal(1), Length(32)]
	public required string Mapping { get; init; }
}
