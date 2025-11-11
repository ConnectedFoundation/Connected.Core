namespace Connected.Storage.Sql.Schemas;

internal class SchemaExists(string name)
	: SynchronizationQuery<bool>
{
	protected override async Task<bool> OnExecute()
	{
		if (string.IsNullOrWhiteSpace(name))
			return true;

		var rdr = await Context.OpenReader(new SqlStorageOperation { CommandText = $"SELECT * FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{name}'" });
		var result = rdr.Read();

		rdr.Close();

		return result;
	}
}
