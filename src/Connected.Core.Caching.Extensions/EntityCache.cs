using Connected.Entities;
using Connected.Reflection;
using Connected.Storage;
using System.Collections.Immutable;

namespace Connected.Caching;

public abstract class EntityCache<TEntity, TPrimaryKey> : SynchronizedCache<TEntity, TPrimaryKey>, IEntityCache<TEntity, TPrimaryKey>
	 where TEntity : class, IPrimaryKeyEntity<TPrimaryKey>, IEntity
	 where TPrimaryKey : notnull
{
	protected EntityCache(ICachingService cache, IStorageProvider storage, string key) : base(cache, key)
	{
		Storage = storage;
	}

	private IStorageProvider Storage { get; }

	protected override sealed async Task OnInitializing()
	{
		if (await OnInitializingEntities() is not ImmutableList<TEntity> ds)
			return;

		foreach (var r in ds)
			Set(r.Id, r, TimeSpan.Zero);
	}

	protected virtual async Task<ImmutableList<TEntity>?> OnInitializingEntities()
	{
		return await (from dc in Storage.Open<TEntity>(StorageConnectionMode.Isolated)
						  select dc).AsEntities();
	}

	protected override async Task OnInvalidate(TPrimaryKey id)
	{
		if (await OnInvalidating(id) is TEntity entity && entity is IPrimaryKeyEntity<TPrimaryKey> pk)
			Set(pk.Id, entity, TimeSpan.Zero, true);
		else
			await Remove(id);
	}

	protected virtual async Task<TEntity?> OnInvalidating(TPrimaryKey id)
	{
		return await (from dc in Storage.Open<TEntity>(StorageConnectionMode.Isolated)
						  where TypeComparer.Compare(dc.Id, id)
						  select dc).AsEntity();
	}

	async Task IEntityCache<TEntity, TPrimaryKey>.Refresh(TPrimaryKey id)
	{
		await ((ICachingDataProvider)this).Invalidate(id);
	}

	async Task IEntityCache<TEntity, TPrimaryKey>.Remove(TPrimaryKey id)
	{
		await Remove(id);
	}

	protected override void Set(TPrimaryKey id, TEntity instance)
	{
		Set(id, instance, TimeSpan.Zero);
	}

	protected override void Set(TPrimaryKey id, TEntity instance, TimeSpan duration)
	{
		if (instance is IConcurrentEntity<TPrimaryKey> concurrent)
		{
			if (Get(id) is TEntity existing && existing is IConcurrentEntity<TPrimaryKey> existingConcurrent)
			{
				lock (existingConcurrent)
				{
					if (existingConcurrent.Sync != concurrent.Sync)
						throw new InvalidOperationException(Strings.ErrConcurrent);

					concurrent.GetType().GetProperty(nameof(IConcurrentEntity<TPrimaryKey>.Sync))?.SetValue(concurrent, concurrent.Sync + 1);

					Set(id, instance, duration);

					return;
				}
			}
		}

		base.Set(id, instance, duration);
	}
}