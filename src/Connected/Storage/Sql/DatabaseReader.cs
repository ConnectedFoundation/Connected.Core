using System.Collections.Immutable;
using System.Data;

namespace Connected.Storage.Sql;

internal class DatabaseReader<T>(IStorageOperation operation, IStorageConnection connection)
	: DatabaseCommand(operation, connection), IStorageReader<T>
{
	public async Task<IImmutableList<T>> Query()
	{
		if (Connection is null)
			return ImmutableList<T>.Empty;

		try
		{
			var result = await Connection.Query<T>(this);

			if (Connection.Mode == StorageConnectionMode.Isolated)
				await Connection.Commit();

			return result;
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

	public async Task<T?> Select()
	{
		try
		{
			if (Connection is null)
				return default;

			var result = await Connection.Select<T>(this);

			if (Connection.Mode == StorageConnectionMode.Isolated)
				await Connection.Commit();

			return result;
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

	public async Task<IDataReader?> OpenReader()
	{
		return await Connection.OpenReader(this);
	}
}
