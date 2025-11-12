namespace Connected.Storage.Sql.Schemas;

internal class ObjectMetaData
{
	public string? Name { get; set; }
	public string? Owner { get; set; }
	public string? Type { get; set; }
	public DateTimeOffset Created { get; set; }
}
