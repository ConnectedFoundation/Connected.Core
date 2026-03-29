using Connected.Caching;
using Connected.Entities;
using Connected.Notifications;
using Connected.Services;
using Connected.Services.Validation;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Connected.Storage;

internal sealed class PatchContext<TEntity, TEntityImplementation, TPrimaryKey>(IStorageProvider storage, IValidationContext validation, IEventService events)
	: IPatchContext<TEntity, TEntityImplementation, TPrimaryKey>
	where TEntity : IEntity<TPrimaryKey>
	where TEntityImplementation : class, TEntity
	where TPrimaryKey : notnull
{
	public async Task<TEntity> Invoke<TDto>(IServiceOperation<IPatchDto<TPrimaryKey>> operation,
		ISelectionService<TEntity, TPrimaryKey> selector,
		ICacheContainer<TEntity, TPrimaryKey>? cache)
		where TDto : IDto
	{
		return await Invoke<TDto>(operation, selector, cache, TransactionContextOptions.All);
	}

	public async Task<TEntity> Invoke<TDto>(IServiceOperation<IPatchDto<TPrimaryKey>> operation,
		ISelectionService<TEntity, TPrimaryKey> selector,
		ICacheContainer<TEntity, TPrimaryKey>? cache,
		TransactionContextOptions options)
		where TDto : IDto
	{
		ValidateProperties<TDto>(operation.Dto);

		var entity = operation.SetState(await selector.Select(operation.Dto.CreatePrimaryKey(operation.Dto.Id)) as TEntityImplementation).Required();
		var st = storage.Open<TEntityImplementation>();
		var updateDto = ServicesExtensions.Patch<TDto, TEntity>(operation.Dto, entity, State.Update);

		await validation.Validate(operation.Caller, updateDto);

		var result = await st.Update(entity, async (f) =>
		{
			updateDto = ServicesExtensions.Patch<TDto, TEntityImplementation>(operation.Dto, entity, State.Update);

			await validation.Validate(operation.Caller, updateDto);

			return entity.Merge(updateDto, State.Update);
		}, async () =>
		{
			if (options.HasFlag(TransactionContextOptions.InvalidateCache) && cache is not null)
			{
				if (cache is IEntityCache<TEntity, TPrimaryKey> entityCache)
					await entityCache.Refresh(operation.Dto.Id);
				else
					await cache.Remove(operation.Dto.Id);
			}

			return operation.SetState(await selector.Select(operation.Dto.CreatePrimaryKey(operation.Dto.Id)) as TEntityImplementation).Required();
		}, operation.Caller, operation.Dto.Properties.Select(f => f.Key));

		if (options.HasFlag(TransactionContextOptions.InvalidateCache) && cache is not null)
		{
			if (cache is IEntityCache<TEntity, TPrimaryKey> entityCache)
				await entityCache.Refresh(operation.Dto.Id);
			else
				await cache.Remove(operation.Dto.Id);
		}

		if (options.HasFlag(TransactionContextOptions.TriggerEvents))
			await events.Updated(operation, selector, operation.Dto.Id);

		return result.Required();
	}

	private static void ValidateProperties<TDto>(IPatchDto<TPrimaryKey> dto)
		where TDto : IDto
	{
		var mock = new Dto<TDto>().Value;

		foreach (var item in dto.Properties)
			_ = mock.GetType().GetProperty(item.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.IgnoreCase) ?? throw new ValidationException($"{SR.ErrInvalidProperty} {typeof(TDto).Name}.{item.Key}");
	}

}
