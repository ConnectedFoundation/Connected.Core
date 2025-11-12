using Connected.Services;

namespace Connected.Storage.Schemas;

public interface IUpdateSchemaDto : IDto
{
	List<Type> Schemas { get; set; }
}