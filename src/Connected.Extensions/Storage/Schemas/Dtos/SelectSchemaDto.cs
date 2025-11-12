using Connected.Annotations;
using Connected.Services;

namespace Connected.Storage.Schemas.Dtos;
internal sealed class SelectSchemaDto : Dto, ISelectSchemaDto
{
	[NonDefault, SkipValidation]
	public required Type Type { get; set; }
}
