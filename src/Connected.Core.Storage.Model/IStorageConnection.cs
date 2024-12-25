using System.Data;

namespace Connected.Storage;

public enum StorageConnectionMode
{
	Shared = 1,
	Isolated = 2
}

public interface IStorageConnection : IMiddleware, IAsyncDisposable, IDisposable
{
	StorageConnectionMode Mode { get; }
	string? ConnectionString { get; }

	Task Initialize(IStorageConnectionDto dto);
	Task Commit();
	Task Rollback();
	Task Close();

	Task<int> Execute(IStorageCommand command);

	Task<ImmutableList<T>> Query<T>(IStorageCommand command);

	Task<T?> Select<T>(IStorageCommand command);

	Task<IDataReader?> OpenReader(IStorageCommand command);
}