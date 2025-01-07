using Connected.Services;
using Connected.Storage.Schemas.Ops;

namespace Connected.Storage.Schemas;

internal class SchemaService(IServiceProvider services) : Service(services), ISchemaService
{
	public async Task Update(IUpdateSchemaDto dto)
	{
		await Invoke(GetOperation<Update>(), dto);
	}

	public async Task<ISchema?> Select(ISelectSchemaDto dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}
}
