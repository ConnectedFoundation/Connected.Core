using System.Collections.Immutable;
using System.Data;

namespace Connected.Storage;

public interface IStorageReader<T> : IStorageCommand
{
	Task<IImmutableList<T>> Query();
	Task<T?> Select();
	Task<IDataReader?> OpenReader();
}
