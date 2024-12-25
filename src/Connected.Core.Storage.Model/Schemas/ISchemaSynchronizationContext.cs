namespace Connected.Storage.Schemas;

public interface ISchemaSynchronizationContext
{
	Type ConnectionType { get; }
	string ConnectionString { get; }
}
