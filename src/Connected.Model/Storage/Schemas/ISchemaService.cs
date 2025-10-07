using Connected.Annotations;

namespace Connected.Storage.Schemas;

[Service]
public interface ISchemaService
{
	Task Update(IUpdateSchemaDto dto);
	Task<ISchema?> Select(ISelectSchemaDto dto);
}
