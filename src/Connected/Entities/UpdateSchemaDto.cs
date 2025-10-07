using Connected.Annotations;
using Connected.Services;
using Connected.Storage.Schemas;

namespace Connected.Entities;
internal sealed class UpdateSchemaDto : Dto, IUpdateSchemaDto
{
	public UpdateSchemaDto()
	{
		Schemas = [];
	}

	[NonDefault]
	public List<Type> Schemas { get; set; }
}
