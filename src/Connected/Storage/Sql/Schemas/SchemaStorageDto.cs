using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

internal sealed class SchemaStorageDto(Type connectionType, string connectionString, IStorageOperation operation)
	: IStorageContextDto, ISchemaSynchronizationContext
{
	public Type ConnectionType { get; set; } = connectionType;
	public string ConnectionString { get; set; } = connectionString;
	public IStorageOperation Operation { get; set; } = operation;
	public StorageConnectionMode ConnectionMode { get; set; } = StorageConnectionMode.Shared;
}
