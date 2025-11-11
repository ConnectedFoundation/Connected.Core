namespace Connected.Storage.Sql.Schemas;

internal class ObjectDescriptor
{
	public ObjectFileGroup FileGroup { get; } = new();
	public ObjectRowGuid RowGuid { get; } = new();
	public ObjectIdentity Identity { get; } = new();
	public ObjectMetaData MetaData { get; } = new();
	public List<ObjectColumn> Columns { get; } = [];
	public List<ObjectIndex> Indexes { get; } = [];
	public List<ObjectConstraint> Constraints { get; } = [];
}
