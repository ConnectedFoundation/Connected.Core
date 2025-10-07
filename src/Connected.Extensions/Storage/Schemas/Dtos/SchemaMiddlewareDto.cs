using Connected.Annotations;
using Connected.Services;

namespace Connected.Storage.Schemas.Dtos;
internal sealed class SchemaMiddlewareDto : Dto, ISchemaMiddlewareDto
{
	[NonDefault, SkipValidation]
	public required Type Type { get; set; }

	[NonDefault, SkipValidation]
	public required ISchema Schema { get; set; }
}
