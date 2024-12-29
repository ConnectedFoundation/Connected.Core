namespace Connected.Storage.Sql;

internal class DatabaseWriter : DatabaseCommand, IStorageWriter
{
	public DatabaseWriter(IStorageOperation operation, IStorageConnection connection)
		: base(operation, connection)
	{
	}

	public async Task<int> Execute()
	{
		try
		{
			var recordsAffected = await Connection.Execute(this);

			if (Connection.Mode == StorageConnectionMode.Isolated)
				await Connection.Commit();

			return recordsAffected;
		}
		finally
		{
			if (Connection.Mode == StorageConnectionMode.Isolated)
			{
				await Connection.Close();
				await Connection.DisposeAsync();
			}
		}
	}
}
