using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

internal sealed class SchemaStorageDto : IStorageContextDto, ISchemaSynchronizationContext
{
	public Type ConnectionType { get; set; }
	public string ConnectionString { get; set; }
	public IStorageOperation Operation { get; set; }
	public StorageConnectionMode ConnectionMode { get; set; }
}
