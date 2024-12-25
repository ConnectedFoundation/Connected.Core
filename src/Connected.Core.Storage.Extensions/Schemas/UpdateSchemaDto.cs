using Connected.Annotations;
using Connected.Services;

namespace Connected.Storage.Schemas;
public class UpdateSchemaDto : Dto, IUpdateSchemaDto
{
	public UpdateSchemaDto()
	{
		Schemas = [];
	}

	[NonDefault]
	public List<Type> Schemas { get; set; }
}
