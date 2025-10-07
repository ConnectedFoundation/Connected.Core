using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Globalization.Languages;

[Table(Schema = SchemaAttribute.CoreSchema)]
internal sealed record Language : ConsistentEntity<int>, ILanguage
{
	[Ordinal(0), Length(32)]
	public required string Name { get; init; }

	[Ordinal(1), Index(true)]
	public int Lcid { get; init; }

	[Ordinal(2)]
	public Status Status { get; init; }

	[Ordinal(3), Length(32)]
	public required string Culture { get; init; }
}
