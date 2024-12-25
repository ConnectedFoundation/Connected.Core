using Connected.Services;

namespace Connected.Storage.Schemas;

public interface ISelectSchemaDto : IDto
{
	Type Type { get; set; }
}