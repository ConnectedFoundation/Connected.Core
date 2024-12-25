using Connected.Services;

namespace Connected.Storage.Schemas;

public interface ISchemaMiddlewareDto : IDto
{
	Type Type { get; set; }
	ISchema Schema { get; set; }
}