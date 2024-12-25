using Connected.Entities;
using Connected.Services;
using System.Collections;
using System.Collections.Immutable;
using System.Data;

namespace Connected.Storage;

/// <summary>
/// Defines the read and write operations on the supported storage providers.
/// </summary>
/// <typeparam name="TEntity">>The type of the entitiy on which operations are performed.</typeparam>
public interface IStorage<TEntity> : IQueryable<TEntity>, IQueryable, IEnumerable<TEntity>, IEnumerable, IOrderedQueryable<TEntity>, IOrderedQueryable
	where TEntity : IEntity
{
	/// <summary>
	/// Updates the entity against the underlying storage.
	/// </summary>
	/// <param name="entity"></param>
	/// <returns></returns>
	Task<TEntity?> Update(TEntity? entity);

	Task<TEntity?> Update<TDto>(TEntity? entity, TDto dto, Func<Task<TEntity?>>? concurrencyRetrying, ICallerContext caller)
		where TDto : IDto;
	Task<TEntity?> Update<TDto>(TEntity? entity, TDto dto, Func<Task<TEntity?>>? concurrencyRetrying, ICallerContext caller, Func<TEntity, Task<TEntity>>? merging)
		where TDto : IDto;

	Task<ImmutableList<IDataReader>> OpenReaders(IStorageContextDto dto);
	Task<int> Execute(IStorageContextDto dto);
	Task<IEnumerable<TEntity>> Query(IStorageContextDto dto);
	Task<TEntity?> Select(IStorageContextDto dto);
}
