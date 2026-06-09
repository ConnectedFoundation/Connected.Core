using Connected.Entities;

namespace Connected.Storage;

public interface IStorageExecutor
{
	Task<IEnumerable<TResult?>> Execute<TResult>(IStorageOperation operation)
		where TResult : IEntity;
}
