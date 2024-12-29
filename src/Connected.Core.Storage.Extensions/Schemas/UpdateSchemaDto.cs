using Connected.Annotations;
using Connected.Services;

namespace Connected.Storage.Schemas;
internal sealed class UpdateSchemaDto : Dto, IUpdateSchemaDto
{
	public UpdateSchemaDto()
	{
		Schemas = [];
	}

	[NonDefault]
	public List<Type> Schemas { get; set; }
}
