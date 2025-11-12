using Connected.Entities;

namespace Connected.Storage;

public interface IStorageExecutor
{
	IEnumerable<TResult?> Execute<TResult>(IStorageOperation operation)
		where TResult : IEntity;
}
