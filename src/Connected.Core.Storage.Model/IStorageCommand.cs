namespace Connected.Storage;

public interface IStorageCommand : IDisposable, IAsyncDisposable
{
	IStorageOperation Operation { get; }
	IStorageConnection Connection { get; }
}
