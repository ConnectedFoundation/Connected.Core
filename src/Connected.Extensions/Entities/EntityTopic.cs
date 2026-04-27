using Connected.Caching;
using Connected.Entities;
using Connected.Notifications;
using Connected.Services;
using Connected.Services.Validation;
using Connected.Storage;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Connected.Entities;

public abstract class EntityTopic<TEntity, TEntityImplementation, TPrimaryKey>(IStorageProvider storage,
	ICacheContainer<TEntity, TPrimaryKey>? cache, IEventService events, IValidationContext validation)
	: IEntityTopic<TEntity, TPrimaryKey>
	where TEntity : IEntity<TPrimaryKey>
	where TEntityImplementation : class, TEntity
	where TPrimaryKey : notnull
{
	protected abstract Task<TEntity?> Select(IPrimaryKeyDto<TPrimaryKey> dto);
	private async Task RefreshCache(TPrimaryKey id)
	{
		if (cache is null)
			return;

		if (cache is IEntityCache<TEntity, TPrimaryKey> entityCache)
			await entityCache.Refresh(id);
		else
			await cache.Remove(id);
	}

	protected virtual bool TriggerEvents => true;
	public async Task Delete(IServiceOperation<IPrimaryKeyDto<TPrimaryKey>> sender)
	{
		var entity = sender.SetState(await Select(sender.Dto)).Required();

		(await storage.Open<TEntityImplementation>().Update(sender.Dto.AsEntity<TEntityImplementation>(State.Delete))).Required();

		await RefreshCache(entity.Id);

		if (TriggerEvents)
			await events.Deleted(sender, sender.Caller.Sender, entity.Id);
	}

	public async Task<TPrimaryKey> Insert<TDto>(IServiceOperation<TDto> sender)
		where TDto : IDto
	{
		var entity = (await storage.Open<TEntityImplementation>().Update(sender.Dto.AsEntity<TEntityImplementation>(State.Add))).Required();

		sender.SetState(entity);

		await RefreshCache(entity.Id);

		if (TriggerEvents)
			await events.Inserted(sender, sender.Caller.Sender, entity.Id);

		return entity.Id;
	}

	public async Task Patch<TUpdateDto>(IServiceOperation<IPatchDto<TPrimaryKey>> sender)
		where TUpdateDto : IPrimaryKeyDto<TPrimaryKey>
	{
		ValidateProperties<TUpdateDto>(sender.Dto);

		var entity = sender.SetState(await Select(sender.Dto.CreatePrimaryKey(sender.Dto.Id)) as TEntityImplementation).Required();
		var st = storage.Open<TEntityImplementation>();
		var updateDto = ServicesExtensions.Patch<TUpdateDto, TEntity>(sender.Dto, entity, State.Update);
		await validation.Validate(sender.Caller, updateDto);

		var result = await st.Update(entity, async (f) =>
		{
			updateDto = ServicesExtensions.Patch<TUpdateDto, TEntityImplementation>(sender.Dto, entity, State.Update);

			await validation.Validate(sender.Caller, updateDto);

			return entity.Merge(updateDto, State.Update);
		}, async () =>
		{
			await RefreshCache(entity.Id);

			return sender.SetState(await Select(sender.Dto.CreatePrimaryKey(sender.Dto.Id)) as TEntityImplementation).Required();
		}, sender.Caller, sender.Dto.Properties.Select(f => f.Key));

		await RefreshCache(entity.Id);

		if (TriggerEvents)
			await events.Updated(sender, sender.Caller.Sender, sender.Dto.Id);
	}

	public async Task Update<TDto>(IServiceOperation<TDto> sender)
		where TDto : IPrimaryKeyDto<TPrimaryKey>
	{
		var entity = sender.SetState(await Select(sender.Dto) as TEntityImplementation).Required();
		var st = storage.Open<TEntityImplementation>();
		var result = await st.Update(entity.Merge(sender.Dto, State.Update), sender.Dto, async () =>
		{
			await RefreshCache(entity.Id);

			return sender.SetState(await Select(sender.Dto) as TEntityImplementation).Required();
		}, sender.Caller);

		await RefreshCache(entity.Id);

		if (TriggerEvents)
			await events.Updated(sender, sender.Caller.Sender, entity.Id);
	}

	private static void ValidateProperties<TUpdateDto>(IPatchDto<TPrimaryKey> dto)
		where TUpdateDto : IDto
	{
		var mock = new Dto<TUpdateDto>().Value;

		foreach (var item in dto.Properties)
			_ = mock.GetType().GetProperty(item.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.IgnoreCase) ?? throw new ValidationException($"{SR.ErrInvalidProperty} {typeof(TUpdateDto).Name}.{item.Key}");
	}
}
