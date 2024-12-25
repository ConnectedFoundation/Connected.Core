namespace Connected.Storage.Schemas;

public interface IExistingSchemaColumn
{
	ImmutableArray<string> QueryIndexColumns(string column);
}
